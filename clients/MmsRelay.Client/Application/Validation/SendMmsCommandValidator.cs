using System;
using FluentValidation;
using MmsRelay.Client.Application.Models;

namespace MmsRelay.Client.Application.Validation;

/// <summary>
/// Validator for SendMmsCommand ensuring all business rules are met
/// </summary>
public sealed class SendMmsCommandValidator : AbstractValidator<SendMmsCommand>
{
    public SendMmsCommandValidator()
    {
        // E.164 phone number validation
        RuleFor(x => x.To)
            .NotEmpty()
            .WithMessage("Phone number is required.")
            .Matches(@"^\+[1-9]\d{1,14}$")
            .WithMessage("Phone number must be in E.164 format (e.g., +15551234567).");

        // Body validation
        RuleFor(x => x.Body)
            .MaximumLength(1600)
            .WithMessage("Message body cannot exceed 1600 characters.");

        // Service URL validation
        RuleFor(x => x.ServiceUrl)
            .NotEmpty()
            .WithMessage("Service URL is required.")
            .Must(BeValidUrl)
            .WithMessage("Service URL must be a valid HTTP or HTTPS URL.");

        // Media URLs validation
        RuleFor(x => x.MediaUrls)
            .Must(BeValidMediaUrls)
            .WithMessage("Media URLs must be comma-separated valid HTTP/HTTPS URLs.")
            .When(x => !string.IsNullOrWhiteSpace(x.MediaUrls));

        // Business rule: Either Body or MediaUrls must be provided
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Body) || !string.IsNullOrWhiteSpace(x.MediaUrls))
            .WithMessage("Either message body or media URLs must be provided.");
    }

    private static bool BeValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool BeValidMediaUrls(string? mediaUrls)
    {
        if (string.IsNullOrWhiteSpace(mediaUrls))
            return true; // Empty is valid when checked conditionally

        var urls = mediaUrls.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        foreach (var url in urls)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || 
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return false;
            }
        }

        return true;
    }
}