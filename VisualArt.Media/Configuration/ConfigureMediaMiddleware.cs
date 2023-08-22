using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VisualArt.Media.Controllers;
using VisualArt.Media.Services;

namespace VisualArt.Media.Configuration
{
    public static class ConfigureMediaMiddleware
    {
        public static IServiceCollection AddMediaServices(this IServiceCollection services, ConfigurationManager configurationManager)
        {
            services
            .Configure<FileStorageService.Options>(configurationManager.GetSection(FileStorageService.Options.SectionName))
            .AddScoped<IFileStorage, FileStorageService>()
            .AddScoped<MediaApiController>()
            .AddSingleton<FileSystemMonitor>()
            .AddHostedService<FileSystemMonitor>();
            return services;
        }
    }
}
