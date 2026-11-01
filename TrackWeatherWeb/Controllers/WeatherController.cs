using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories.Interfaces;

namespace TrackWeatherWeb.Controllers
{
    [Route("api/weather")]
    public class WeatherController(IWeatherRepo _weatherRepo) : ApiControllerBase
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
    }
}
