using System;
using System.Collections.Generic;

namespace MmsRelay.Client.Application.Models;

/// <summary>
/// Request model for sending MMS messages through the MmsRelay service
/// </summary>
public sealed class SendMmsRequest
{
    /// <summary>
    /// Recipient phone number in E.164 format (e.g., +15551234567)
    /// </summary>
    public required string To { get; init; }

    /// <summary>
    /// Message body text (optional if MediaUrls provided)
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Collection of media URLs to include in the MMS (optional if Body provided)
    /// </summary>
    public IReadOnlyList<Uri>? MediaUrls { get; init; }
}