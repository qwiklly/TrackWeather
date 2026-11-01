namespace TrackWeatherWeb.Infrastructure
{
    public static class SecurityHeaders
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
            app.Use(async (context, next) =>
            {
                var headers = context.Response.Headers;
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "geolocation=(self), camera=(), microphone=()";
                // Yandex Maps needs inline scripts and eval, so script-src stays open; these directives still apply
                headers.ContentSecurityPolicy = "frame-ancestors 'none'; object-src 'none'; base-uri 'self'; form-action 'self'";
                await next();
            });
    }
}
