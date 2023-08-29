using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Net;
using VisualArt.Media.Exceptions;

namespace VisualArt.Media.Configuration
{
    public static class WebApplicationExtensions
    {
        public static WebApplication ConfigureMediaServicesExceptionHandler(this WebApplication app)
        {
            app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                        var exception = exceptionHandlerFeature!.Error;

                        var response = new { message = exception.Message }; // Not safe to expose exception message to client

                        switch (exception)
                        {
                            case PathTooLongException:
                            case ArgumentException:
                                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                break;
                            case HttpResponseException x:
                                context.Response.StatusCode = (int)x.StatusCode;
                                break;
                            default:
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                break;
                        }

                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(response);
                    });
                });
            return app;
        }
    }
}
