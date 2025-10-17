using System;

namespace MmsRelay.Application.Models;

public sealed class SendMmsResult
{
    public required string Provider { get; init; }
    public required string ProviderMessageId { get; init; }
    public required string Status { get; init; }
    public Uri? ProviderMessageUri { get; init; }
}
