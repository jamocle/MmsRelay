using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MmsRelay.Application;
using MmsRelay.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MmsRelay.Infrastructure.Twilio;

public sealed class TwilioMmsSender(HttpClient http, IOptions<TwilioOptions> options, ILogger<TwilioMmsSender> logger)
    : IMmsSender
{
    private readonly HttpClient _http = http;
    private readonly TwilioOptions _opts = options.Value;
    private readonly ILogger<TwilioMmsSender> _logger = logger;

    public async Task<SendMmsResult> SendAsync(SendMmsRequest request, CancellationToken ct)
    {
        var accountSid = _opts.AccountSid;
        var authToken  = _opts.AuthToken;
        if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
            throw new InvalidOperationException("Twilio AccountSid/AuthToken must be configured.");

        var endpoint = $"{_opts.BaseUrl}/Accounts/{WebUtility.UrlEncode(accountSid)}/Messages.json";

        var fields = new List<KeyValuePair<string, string>>();

        if (!string.IsNullOrWhiteSpace(request.Body))
            fields.Add(new("Body", request.Body!));

        if (!string.IsNullOrWhiteSpace(_opts.MessagingServiceSid))
            fields.Add(new("MessagingServiceSid", _opts.MessagingServiceSid!));
        else if (!string.IsNullOrWhiteSpace(_opts.FromPhoneNumber))
            fields.Add(new("From", _opts.FromPhoneNumber!));
        else
            throw new InvalidOperationException("Either Twilio MessagingServiceSid or FromPhoneNumber must be configured.");

        fields.Add(new("To", request.To));

        if (request.MediaUrls is { Count: > 0 })
        {
            // FIX: avoid name collision with 'uri' later by renaming loop variable.
            foreach (var mediaUri in request.MediaUrls)
                fields.Add(new("MediaUrl", mediaUri.ToString()));
        }

        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        var authValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        req.Content = new FormUrlEncodedContent(fields);

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        var content = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Twilio send failed: {Status} {Content}", (int)resp.StatusCode, Truncate(content, 1024));
            throw new TwilioApiException((int)resp.StatusCode, content);
        }

        using var doc = JsonDocument.Parse(content);
        string sid = doc.RootElement.GetProperty("sid").GetString()!;
        string status = doc.RootElement.TryGetProperty("status", out var statusEl)
            ? statusEl.GetString() ?? "unknown"
            : "queued";

        // This local variable is named 'messageUri' now, not 'uri', to avoid shadowing.
        Uri? messageUri = doc.RootElement.TryGetProperty("uri", out var uriEl)
            ? TryParseUri("https://api.twilio.com" + uriEl.GetString())
            : null;

        return new SendMmsResult
        {
            Provider = "twilio",
            ProviderMessageId = sid,
            Status = status,
            ProviderMessageUri = messageUri
        };
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    private static Uri? TryParseUri(string? value)
        => Uri.TryCreate(value, UriKind.Absolute, out var u) ? u : null;

    public sealed class TwilioApiException(int statusCode, string responseContent)
        : Exception($"Twilio API error (HTTP {statusCode})")
    {
        public int StatusCode { get; } = statusCode;
        public string ResponseContent { get; } = responseContent;
    }
}
