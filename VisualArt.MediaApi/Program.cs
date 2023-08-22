using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using VisualArt.Media.Configuration;
using VisualArt.Media.Controllers;

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
            builder.Services.AddSwaggerGen(c =>
            {
                c.OperationFilter<MakeRouteParameterOptional>("path");
            });
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
                    mediaController.UploadFiles(path ?? "", files));

            app.MapGet("/api/media/metadata/{*path}",
                ([FromServices] MediaApiController mediaController, string? path, uint? start, uint? count) =>
                    mediaController.ListFiles(path ?? "", start ?? 0, count ?? int.MaxValue));

            app.Run();
        }
    }

    public class MakeRouteParameterOptional : IOperationFilter
    {
        readonly string _name;
        public MakeRouteParameterOptional(string name)
        {
            _name = name;
        }
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var parameter = operation.Parameters.FirstOrDefault(p => p.Name == _name);
            if (parameter != null)
            {
                parameter.AllowEmptyValue = true;
                parameter.Required = false;
                parameter.Description = "Must check \"Send empty value\" or Swagger passes a comma for empty values";
            }
        }
    }
}