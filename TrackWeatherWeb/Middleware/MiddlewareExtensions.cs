namespace TrackWeatherWeb.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalException>();
        }
    }
}
