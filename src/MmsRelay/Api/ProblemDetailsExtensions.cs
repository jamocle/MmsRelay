using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MmsRelay.Api;

public static class ProblemDetailsExtensions
{
    public static void UseProblemDetails(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerPathFeature>();
                var exception = feature?.Error;

                var status = exception switch
                {
                    ArgumentException => (int)HttpStatusCode.BadRequest,
                    InvalidOperationException => (int)HttpStatusCode.BadRequest,
                    _ => (int)HttpStatusCode.InternalServerError
                };

                var pd = new ProblemDetails
                {
                    Title = "An error occurred while processing your request.",
                    Status = status,
                    Type = "about:blank",
                    Detail = app.Environment.IsDevelopment() ? exception?.Message : null,
                    Instance = context.Request.Path
                };

                context.Response.ContentType = "application/problem+json";
                context.Response.StatusCode = status;
                await context.Response.WriteAsJsonAsync(pd);
            });
        });
    }
}
