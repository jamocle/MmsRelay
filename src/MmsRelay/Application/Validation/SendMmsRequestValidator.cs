using System;
using FluentValidation;
using MmsRelay.Application.Models;

namespace MmsRelay.Application.Validation;

public sealed class SendMmsRequestValidator : AbstractValidator<SendMmsRequest>
{
    public SendMmsRequestValidator()
    {
        RuleFor(x => x.To)
            .NotEmpty()
            .Matches(@"^\+[1-9]\d{1,14}$")
            .WithMessage("To must be E.164, e.g., +15551234567.");

        RuleFor(x => x.Body)
            .MaximumLength(1600);

        // FIX: expression trees can't contain C# collection expressions ([]).
        // Use Array.Empty<Uri>() which is allowed inside the expression tree.
        RuleForEach(x => x.MediaUrls ?? Array.Empty<Uri>())
            .Must(uri => uri.Scheme is "http" or "https")
            .WithMessage("MediaUrls must be HTTP/HTTPS.");

        // Require either Body or MediaUrls
        RuleFor(x => x)
            .Must(x => !(string.IsNullOrWhiteSpace(x.Body) && (x.MediaUrls is null || x.MediaUrls.Count == 0)))
            .WithMessage("Either Body or at least one MediaUrl must be provided.");
    }
}
