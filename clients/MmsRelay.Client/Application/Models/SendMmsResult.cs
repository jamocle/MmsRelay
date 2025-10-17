using System;

namespace MmsRelay.Client.Application.Models;

/// <summary>
/// Response model from the MmsRelay service after sending an MMS
/// </summary>
public sealed class SendMmsResult
{
    /// <summary>
    /// The messaging provider that handled the request (e.g., "twilio")
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// The unique message identifier from the provider
    /// </summary>
    public required string ProviderMessageId { get; init; }

    /// <summary>
    /// Current status of the message (e.g., "queued", "sent", "delivered")
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Optional URI to check message status in the provider's system
    /// </summary>
    public Uri? ProviderMessageUri { get; init; }
}