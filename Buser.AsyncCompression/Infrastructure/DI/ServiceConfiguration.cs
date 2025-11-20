using Microsoft.Extensions.DependencyInjection;
using Buser.AsyncCompression.Application.Factories;
using Buser.AsyncCompression.Application.Services;
using Buser.AsyncCompression.Domain.Interfaces;
using Buser.AsyncCompression.Infrastructure.Algorithms;
using Buser.AsyncCompression.Infrastructure.Services;

namespace Buser.AsyncCompression.Infrastructure.DI
{
    public static class ServiceConfiguration
    {
        public static ServiceProvider ConfigureServices(IProgressReporter? progressReporter = null)
        {
            var services = new ServiceCollection();

            // Register services
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<ICompressionAlgorithm, GZipCompressionAlgorithm>();
            services.AddTransient<ICompressionService, CompressionService>();

            // Register progress reporter if provided
            if (progressReporter != null)
            {
                services.AddSingleton<IProgressReporter>(progressReporter);
            }

            // Register factories
            services.AddSingleton<CompressionJobFactory>();
            services.AddSingleton<CompressionAlgorithmFactory>();

            // Register application services
            services.AddTransient<CompressionApplicationService>();

            return services.BuildServiceProvider();
        }
    }
}
