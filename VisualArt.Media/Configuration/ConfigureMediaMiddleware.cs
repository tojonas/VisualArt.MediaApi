using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VisualArt.Media.Controllers;
using VisualArt.Media.Services;

namespace VisualArt.Media.Configuration
{
    public static class ConfigureMediaMiddleware
    {
        public static void AddMediaServices(this IServiceCollection services, ConfigurationManager configurationManager)
        {
            services.Configure<FileStorageService.Options>(configurationManager.GetSection(FileStorageService.Options.SectionName));
            //services.Configure<FileSystemMonitor.Options>(configurationManager.GetSection(FileSystemMonitor.Options.SectionName));

            services.AddScoped<IFileStorage, FileStorageService>();
            services.AddScoped<MediaApiController>();
            services.AddSingleton<FileSystemMonitor>();
            services.AddHostedService<FileSystemMonitor>();
        }
    }
}
