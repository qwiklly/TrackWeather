using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Net;
using System.Text.Json;

namespace TrackWeatherWeb.Middleware
{
    public class GlobalException(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            //Declaire default variables
            string message = "server error occurred";
            int statusCode = (int)HttpStatusCode.InternalServerError;
            string title = "Error";

            try
            {
                await next(context);

                // cheack if Response have many requests (429)
                if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
                {
                    title = "Warning";
                    message = "Too many requests made";
                    statusCode = StatusCodes.Status429TooManyRequests;
                    await ModifyHeader(context, title, message, statusCode);

                }
                // cheack if Response is not Authorized (401)
                if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
                {
                    title = "Warning";
                    message = "You aren't Authorized";
                    statusCode = StatusCodes.Status401Unauthorized;
                    await ModifyHeader(context, title, message, statusCode);
                }
                // cheack if Response is Forbidden (403)
                if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
                {
                    title = "Warning";
                    message = "You are not allowed to access";
                    statusCode = StatusCodes.Status403Forbidden;
                    await ModifyHeader(context, title, message, statusCode);
                }
            }
            catch (Exception ex)
            {
                // Log Original Exceptions
                Log.Error(ex, "error");
                // cheack if Ex is Timeout(408)
                if (ex is TaskCanceledException || ex is TimeoutException)
                {
                    title = "Out of Time";
                    message = "request timeout please try again";
                    statusCode = StatusCodes.Status408RequestTimeout;

                }
                //If none or Exception caught
                await ModifyHeader(context, title, message, statusCode);
            }
        }

        private static async Task ModifyHeader(HttpContext context, string title, string message, int statusCode)
        {
            // display message to client
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails()
            {
                Detail = message,
                Title = title,
                Status = statusCode,
            }), CancellationToken.None);
            return;
        }
    }
}

