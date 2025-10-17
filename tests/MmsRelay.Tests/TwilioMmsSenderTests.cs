using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MmsRelay.Application.Models;
using MmsRelay.Infrastructure.Twilio;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MmsRelay.Tests;

[TestClass]
public class TwilioMmsSenderTests
{
    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }

    [TestMethod]
    public async Task SendAsync_ReturnsResult_On200()
    {
        var json = """
        { "sid": "SM123", "status": "queued", "uri": "/2010-04-01/Accounts/ACx/Messages/SM123.json" }
        """;

        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.twilio.com/2010-04-01") };

        var options = Options.Create(new TwilioOptions
        {
            AccountSid = "ACxxxx",
            AuthToken = "token",
            FromPhoneNumber = "+15551234567"
        });

        var sender = new TwilioMmsSender(http, options, NullLogger<TwilioMmsSender>.Instance);

        var req = new SendMmsRequest
        {
            To = "+15557654321",
            Body = "hi",
            MediaUrls = new[] { new Uri("https://example.com/a.jpg") }
        };

        var result = await sender.SendAsync(req, CancellationToken.None);

        result.Provider.Should().Be("twilio");
        result.ProviderMessageId.Should().Be("SM123");
        result.Status.Should().Be("queued");
        result.ProviderMessageUri!.ToString().Should().Contain("SM123.json");
    }

    [TestMethod]
    public async Task SendAsync_Throws_OnNon2xx()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"message\":\"bad\"}")
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.twilio.com/2010-04-01") };

        var options = Options.Create(new TwilioOptions
        {
            AccountSid = "ACxxxx",
            AuthToken = "token",
            FromPhoneNumber = "+15551234567"
        });

        var sender = new TwilioMmsSender(http, options, NullLogger<TwilioMmsSender>.Instance);
        var req = new SendMmsRequest { To = "+15557654321", Body = "hi" };

        Func<Task> act = async () => await sender.SendAsync(req, CancellationToken.None);

        await act.Should().ThrowAsync<TwilioMmsSender.TwilioApiException>();
    }
}
