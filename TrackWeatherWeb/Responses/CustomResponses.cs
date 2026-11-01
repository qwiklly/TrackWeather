using System.Text.Json.Serialization;

namespace TrackWeatherWeb.Responses
{
    public class CustomResponses
    {
        public enum ResponseError
        {
            None,
            NotFound,
            Conflict,
            Forbidden,
            Unauthorized
        }

        public record BaseResponse(bool Flag = false, string Message = null!, [property: JsonIgnore] ResponseError Error = ResponseError.None);
        public record RegisterResponse(bool Flag = false, string Message = null!, ResponseError Error = ResponseError.None) : BaseResponse(Flag, Message, Error);
        public record LoginResponse(bool Flag = false, string Message = null!, string JWTToken = null!, ResponseError Error = ResponseError.None) : BaseResponse(Flag, Message, Error);
    }
}
