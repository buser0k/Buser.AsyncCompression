using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

            // Register logging
            // Important: minimise logging to console to avoid breaking the ProgressBar output.
            // Only errors are written, and they go to standard error.
            services.AddLogging(builder =>
            {
                builder.AddConsole(options =>
                {
                    options.LogToStandardErrorThreshold = LogLevel.Error;
                });
                builder.SetMinimumLevel(LogLevel.Error);
            });

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
