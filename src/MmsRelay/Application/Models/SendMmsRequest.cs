using System;
using System.Collections.Generic;

namespace MmsRelay.Application.Models;

public sealed class SendMmsRequest
{
    public required string To { get; init; }
    public string? Body { get; init; }
    public IReadOnlyList<Uri>? MediaUrls { get; init; }
}
