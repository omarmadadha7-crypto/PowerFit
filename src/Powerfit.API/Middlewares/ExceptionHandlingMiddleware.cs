using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Powerfit.API.Responses;
using Powerfit.Business.Exceptions;

namespace Powerfit.API.Middlewares;

/// <summary>
/// Turns the Business exceptions into JSON error responses.
/// Register it FIRST in the pipeline (app.UseMiddleware&lt;ExceptionHandlingMiddleware&gt;()).
///   ValidationException -> 400 | UnauthorizedException -> 401 | NotFoundException -> 404
///   ConflictException -> 409 | TooManyRequestsException -> 429 (+ Retry-After)
///   AiGenerationException -> 502 (504 on timeout) | anything else -> 500
/// Database conflicts that slip past the pre-checks (duplicate value, row in use, row changed by someone else)
/// are translated to ConflictException first, so they answer 409 instead of 500.
/// </summary>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client closed the connection: nobody is waiting for an answer.
            logger.LogInformation("Request cancelled by the client: {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (!context.Response.HasStarted)
                context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError(ex, "Unhandled exception after the response had started.");
                throw;
            }

            var error = ExceptionTranslator.Translate(ex);
            var response = Map(context, error);
            Log(error, response.Status);

            if (response.Status == StatusCodes.Status401Unauthorized)
                context.Response.Headers["WWW-Authenticate"] = "Bearer";

            if (error is TooManyRequestsException { RetryAfter: { } wait })
            {
                context.Response.Headers["Retry-After"] =
                    ((int)Math.Ceiling(wait.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
            }

            await response.WriteToAsync(context);
        }
    }

    private ErrorResponse Map(HttpContext context, Exception ex) => ex switch
    {
        ValidationException v => ErrorResponse.From(context, 400, "Validation failed", v.Message, v.Errors),
        UnauthorizedException => ErrorResponse.From(context, 401, "Unauthorized", ex.Message),
        NotFoundException => ErrorResponse.From(context, 404, "Not found", ex.Message),
        ConflictException => ErrorResponse.From(context, 409, "Conflict", ex.Message),
        TooManyRequestsException => ErrorResponse.From(context, 429, "Too many requests", ex.Message),
        AiGenerationException ai when ai.IsTimeout => ErrorResponse.From(context, 504, "AI timeout", ai.Message),
        AiGenerationException ai => ErrorResponse.From(context, 502, "AI generation failed", ai.Message),
        BadHttpRequestException bad => ErrorResponse.From(context, bad.StatusCode, "Bad request", bad.Message),
        _ => ErrorResponse.From(context, 500, "Server error",
            environment.IsDevelopment() ? ex.Message : "An unexpected error occurred.")
    };

    private void Log(Exception ex, int status)
    {
        if (status >= 500 && ex is not AiGenerationException)
            logger.LogError(ex, "Unhandled exception ({Status}).", status);
        else if (ex is AiGenerationException)
            logger.LogWarning("AI generation problem ({Status}): {Message}", status, ex.Message);
        else
            logger.LogInformation("Request rejected ({Status}): {Message}", status, ex.Message);
    }
}
