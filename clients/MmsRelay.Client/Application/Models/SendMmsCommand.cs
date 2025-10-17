using System;

namespace MmsRelay.Client.Application.Models;

/// <summary>
/// Command line arguments for sending MMS messages
/// </summary>
public sealed record SendMmsCommand
{
    /// <summary>
    /// Recipient phone number in E.164 format
    /// </summary>
    public required string To { get; init; }

    /// <summary>
    /// Message body text
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Comma-separated list of media URLs
    /// </summary>
    public string? MediaUrls { get; init; }

    /// <summary>
    /// Base URL of the MmsRelay service
    /// </summary>
    public string ServiceUrl { get; init; } = "http://localhost:8080";

    /// <summary>
    /// Enable verbose logging output
    /// </summary>
    public bool Verbose { get; init; } = false;
}