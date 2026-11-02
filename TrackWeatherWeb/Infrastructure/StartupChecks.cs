using Microsoft.Extensions.Options;
using TrackWeatherWeb.Options;

namespace TrackWeatherWeb.Infrastructure
{
    public static class StartupChecks
    {
        // A missing key only breaks one feature later, so it is easy to miss without a clear note at startup
        public static void LogMissingSettings(this WebApplication app)
        {
            var hint = app.Environment.IsDevelopment()
                ? "Set it with `dotnet user-secrets` in the TrackWeatherWeb folder."
                : "Set it through an environment variable.";

            if (string.IsNullOrWhiteSpace(app.Configuration[$"{JwtOptions.SectionName}:Key"]))
            {
                app.Logger.LogWarning("Jwt:Key is not configured, using a temporary key: API tokens stop working after a restart. {Hint}", hint);
            }
            if (string.IsNullOrWhiteSpace(app.Services.GetRequiredService<IOptions<YandexMapsOptions>>().Value.ApiKey))
            {
                app.Logger.LogWarning("YandexMaps:ApiKey is not configured, the map will not load. {Hint}", hint);
            }
            if (string.IsNullOrWhiteSpace(app.Services.GetRequiredService<IOptions<OpenWeatherOptions>>().Value.ApiKey))
            {
                app.Logger.LogWarning("OpenWeatherApi:ApiKey is not configured, weather will be unavailable. {Hint}", hint);
            }
        }
    }
}
