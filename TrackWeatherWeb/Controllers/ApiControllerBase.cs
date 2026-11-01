using Microsoft.AspNetCore.Mvc;
using static TrackWeatherWeb.Responses.CustomResponses;

namespace TrackWeatherWeb.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public abstract class ApiControllerBase : ControllerBase
    {
        // Successful operations return the response, failures become ProblemDetails with a matching status code
        protected ActionResult FromResponse(BaseResponse response, int successStatus = StatusCodes.Status200OK)
        {
            if (response.Flag)
            {
                return successStatus == StatusCodes.Status200OK ? Ok(response) : StatusCode(successStatus, response);
            }

            var status = response.Error switch
            {
                ResponseError.NotFound => StatusCodes.Status404NotFound,
                ResponseError.Conflict => StatusCodes.Status409Conflict,
                ResponseError.Forbidden => StatusCodes.Status403Forbidden,
                ResponseError.Unauthorized => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status400BadRequest,
            };

            return new ObjectResult(new ProblemDetails { Status = status, Detail = response.Message }) { StatusCode = status };
        }
    }
}
