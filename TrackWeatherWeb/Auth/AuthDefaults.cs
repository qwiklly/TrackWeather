namespace TrackWeatherWeb.Auth
{
    public static class AuthDefaults
    {
        public const string CookieOrBearerScheme = "CookieOrBearer";
        public const string CookieName = ".TrackWeather.Auth";
        public const string RateLimitPolicy = "auth";
        public const string UserActionsPolicy = "user-actions";
    }
}
