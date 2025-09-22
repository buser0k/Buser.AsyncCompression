using Microsoft.Extensions.DependencyInjection;
using Booser.AsyncCompression.Application.Factories;
using Booser.AsyncCompression.Application.Services;
using Booser.AsyncCompression.Domain.Interfaces;
using Booser.AsyncCompression.Infrastructure.Algorithms;
using Booser.AsyncCompression.Infrastructure.Services;

namespace Booser.AsyncCompression.Infrastructure.DI
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

            return services.BuildServiceProvider();
        }
    }
}
