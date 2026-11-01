using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Localization;
using Polly.CircuitBreaker;
using Polly.Timeout;
using TrackWeatherWeb.Repositories;

namespace TrackWeatherWeb.Infrastructure
{
    // Unhandled API errors become ProblemDetails with the right status code; pages fall through to /Error
    public class ApiExceptionHandler(
        IProblemDetailsService problemDetails,
        IStringLocalizer<SharedResource> localizer,
        ILogger<ApiExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            // Request.Path may already point at the error page, the feature keeps the original one
            var path = new PathString(context.Features.Get<IExceptionHandlerPathFeature>()?.Path ?? context.Request.Path);
            if (!path.StartsWithSegments("/api"))
            {
                return false;
            }

            var (status, messageKey) = exception switch
            {
                WeatherUnavailableException or HttpRequestException or BrokenCircuitException or TimeoutRejectedException
                    => (StatusCodes.Status503ServiceUnavailable, "Error.WeatherUnavailable"),
                OperationCanceledException or TimeoutException
                    => (StatusCodes.Status408RequestTimeout, "Error.Timeout"),
                _ => (StatusCodes.Status500InternalServerError, "Error.Server"),
            };

            logger.LogError(exception, "Unhandled API exception on {Path}", path);

            context.Response.StatusCode = status;
            return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                Exception = exception,
                ProblemDetails = { Status = status, Detail = localizer[messageKey] },
            });
        }
    }
}
