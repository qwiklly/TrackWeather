using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using TrackWeatherWeb.Auth;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories.Interfaces;
using TrackWeatherWeb.Services;

namespace TrackWeatherWeb.Controllers
{
    [Route("api/weather")]
    public class WeatherController(IWeatherRepo _weatherRepo, IRouteWeatherService _routeWeather) : ApiControllerBase
    {
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get current weather",
            Description = "Current weather at a point. The description follows the Accept-Language header or the chosen site language."
        )]
        [ProducesResponseType<WeatherDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult> GetWeatherAsync(
            [FromQuery, Range(-90, 90)] double lat,
            [FromQuery, Range(-180, 180)] double lon)
        {
            var result = await _weatherRepo.GetCurrentWeatherAsync(lat, lon, SupportedLanguages.Current);
            return Ok(result);
        }

        [HttpPost("route")]
        [EnableRateLimiting(AuthDefaults.UserActionsPolicy)]
        [SwaggerOperation(
            Summary = "Assess the weather along a route",
            Description = "Takes the route line with the planned travel time to each point, splits it into stretches and rates each one by the forecast for the moment of arrival."
        )]
        [ProducesResponseType<RouteWeatherDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult> AnalyzeRouteAsync(RouteWeatherRequestDTO model, CancellationToken cancellationToken)
        {
            var result = await _routeWeather.AnalyzeAsync(model, SupportedLanguages.Current, cancellationToken);
            return Ok(result);
        }
    }
}
