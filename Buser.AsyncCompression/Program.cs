using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Buser.AsyncCompression.Application.Factories;
using Buser.AsyncCompression.Application.Services;
using Buser.AsyncCompression.Domain.Entities;
using Buser.AsyncCompression.Domain.Interfaces;
using Buser.AsyncCompression.Domain.ValueObjects;
using Buser.AsyncCompression.Infrastructure.DI;

namespace Buser.AsyncCompression
{
    static class Program
    {
        private const int BlockSize = 1024 * 8;

        static async Task<int> Main(string[] args)
        {
            // For testing, use a hardcoded file name
            string inputFile = "test.txt";
            
            if (args.Length > 0 && !args[0].EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                inputFile = args[0];
            }

            // Configure dependency injection
            var progressReporter = new ProgressBar();
            using var serviceProvider = ServiceConfiguration.ConfigureServices(progressReporter);
            
            // Get CompressionApplicationService from DI container
            var applicationService = serviceProvider.GetRequiredService<CompressionApplicationService>();
            var settings = new CompressionSettings(BlockSize);

            var stopwatch = Stopwatch.StartNew();

            Console.Write("Press <P> to pause, <R> to resume or <X> to interrupt the compression process...\b\n");
            Console.Write("File compression in progress...\b\n");

            using (progressReporter)
            {
                Console.WriteLine($"Compressing file: {inputFile}");
                
                // Create job first so we can track it for pause/resume/cancel
                var job = applicationService.CreateJob(inputFile, settings);
                var compressionTask = applicationService.CompressFileAsync(job);
                var keyReaderTask = StartKeyReaderAsync(applicationService, job, compressionTask);

                try
                {
                    var result = await compressionTask;
                    
                    if (result.IsSuccess)
                    {
                        stopwatch.Stop();
                        Console.WriteLine("Done in {0}s", stopwatch.Elapsed.TotalSeconds);
                        return 0;
                    }
                    else
                    {
                        Console.WriteLine("Error: {0}", result.ErrorMessage);
                        return -1;
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine();
                    Console.WriteLine("Cancelled");
                    return -1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error: {0}", ex.Message);
                    return -1;
                }
                finally
                {
                    job?.Dispose();
                }
            }
        }

        private static async Task StartKeyReaderAsync(
            CompressionApplicationService applicationService, 
            Domain.Entities.CompressionJob job,
            Task<CompressionResult> compressionTask)
        {
            await Task.Run(() =>
            {
                while (!compressionTask.IsCompleted)
                {
                    var key = Console.ReadKey(true);

                    switch (key.Key)
                    {
                        case ConsoleKey.P:
                            Console.Title = "Paused... Press R to resume.";
                            applicationService.PauseCompression(job);
                            break;
                        case ConsoleKey.R:
                            Console.Title = "In progress... Press P to pause.";
                            applicationService.ResumeCompression(job);
                            break;
                        case ConsoleKey.X:
                            Console.Title = "Cancelling...";
                            applicationService.CancelCompression(job);
                            break;
                        default:
                            continue;
                    }
                }
            });
        }
    }
}
