using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using System.ComponentModel.DataAnnotations;
using System.Net;
using VisualArt.Media.Configuration;
using VisualArt.Media.Controllers;
using VisualArt.Media.Exceptions;

namespace VisualArt.MediaApi
{
    // Use /ui/upload.html to upload files
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = builder.Configuration.GetValue<long>("MultipartBodyLengthLimit");
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddMediaServices(builder.Configuration);
            var app = builder.Build();

            app.ConfigureMediaServicesExceptionHandler();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                    options.RoutePrefix = string.Empty;
                });
            }
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles")),
                RequestPath = "/ui",
                EnableDefaultFiles = true
            });

            app.MapPost("/api/media/{*path}", 
                ([FromServices] MediaApiController mediaController, string? path, IFormFileCollection files) => 
                    mediaController.UploadFiles(path ?? "",files));

            app.MapGet("/api/media/metadata/{*path}", 
                ([FromServices] MediaApiController mediaController, string? path, uint? start, uint? count) => 
                    mediaController.ListFiles(path ?? "", start ?? 0, count ?? int.MaxValue));

            app.Run();
        }
    }
}