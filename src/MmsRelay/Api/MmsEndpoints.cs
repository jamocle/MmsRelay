using FluentValidation;
using FluentValidation.Results;
using MmsRelay.Application;
using MmsRelay.Application.Models;

namespace MmsRelay.Api;

public static class MmsEndpoints
{
    public static void MapMmsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/mms", async (
            SendMmsRequest request,
            IValidator<SendMmsRequest> validator,
            IMmsSender sender,
            CancellationToken cancellationToken) =>
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await sender.SendAsync(request, cancellationToken);
            return Results.Accepted($"/mms/{result.Provider}/{result.ProviderMessageId}", result);
        })
        .WithName("RelayMms")
        .WithSummary("Send MMS via configured provider")
        .WithDescription("Sends an MMS message through the configured provider (e.g., Twilio). Requires either body text or media URLs.")
        .Produces<SendMmsResult>(202)
        .ProducesProblem(400)
        .ProducesProblem(500)
        .WithTags("MMS");
    }
}