using System;
using System.Diagnostics;
using System.IO;
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
        static async Task<int> Main(string[] args)
        {
            if (args.Length == 0 || IsHelpRequested(args))
            {
                PrintUsage();
                return 0;
            }

            // Parse command-line arguments
            bool singleArchive = false;
            string? inputPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                
                // Ignore the executable name when running single-file deployment
                if (arg.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (arg.Equals("--single-archive", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("-a", StringComparison.OrdinalIgnoreCase))
                {
                    singleArchive = true;
                }
                else if (!arg.StartsWith("-") && string.IsNullOrEmpty(inputPath))
                {
                    inputPath = arg;
                }
            }

            if (string.IsNullOrEmpty(inputPath))
            {
                Console.WriteLine("Error: Input path is required.");
                PrintUsage();
                return -1;
            }

            // Configure dependency injection
            var progressReporter = new ProgressBar();
            using var serviceProvider = ServiceConfiguration.ConfigureServices(progressReporter);
            
            // Get CompressionApplicationService from DI container
            var applicationService = serviceProvider.GetRequiredService<CompressionApplicationService>();
            // Use default settings from CompressionSettings
            var settings = CompressionSettings.Default;

            var stopwatch = Stopwatch.StartNew();

            using (progressReporter)
            {
                var isDirectory = Directory.Exists(inputPath);
                var isFile = File.Exists(inputPath);

                if (!isDirectory && !isFile)
                {
                    Console.WriteLine($"Input path not found: {inputPath}");
                    return -1;
                }

                if (isDirectory)
                {
                    if (singleArchive)
                    {
                        var archiveResult = await CompressDirectoryToSingleArchiveAsync(applicationService, inputPath, settings, stopwatch);
                        return archiveResult;
                    }
                    else
                    {
                        var directoryResult = await CompressDirectoryAsync(applicationService, inputPath, settings);
                        if (directoryResult == 0)
                        {
                            stopwatch.Stop();
                            Console.WriteLine("Directory compression completed in {0}s", stopwatch.Elapsed.TotalSeconds);
                        }
                        return directoryResult;
                    }
                }

                return await CompressSingleFileAsync(applicationService, inputPath, settings, stopwatch);
            }
        }
        private static bool IsHelpRequested(string[] args)
        {
            return args.Length > 0 && (args[0].Equals("-h", StringComparison.OrdinalIgnoreCase)
                || args[0].Equals("--help", StringComparison.OrdinalIgnoreCase)
                || args[0].Equals("/?", StringComparison.OrdinalIgnoreCase));
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Buser.AsyncCompression - asynchronous file and directory compression");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run <path_to_file>");
            Console.WriteLine("  dotnet run <path_to_directory> [--single-archive|-a]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --single-archive, -a    Pack directory into a single archive file (tar.gz)");
            Console.WriteLine("                         preserving internal structure");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run C:\\data\\report.csv");
            Console.WriteLine("  dotnet run ./logs");
            Console.WriteLine("  dotnet run ./logs --single-archive");
            Console.WriteLine("  dotnet run ./logs -a");
            Console.WriteLine();
            Console.WriteLine("The application automatically detects whether the path is a file or a directory.");
            Console.WriteLine("Use -h or --help to display this message.");
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

        private static async Task<int> CompressSingleFileAsync(
            CompressionApplicationService applicationService,
            string inputFile,
            CompressionSettings settings,
            Stopwatch stopwatch)
        {
            Console.Write("Press <P> to pause, <R> to resume or <X> to interrupt the compression process...\b\n");
            Console.Write("File compression in progress...\b\n");
            Console.WriteLine($"Compressing file: {inputFile}");

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

                Console.WriteLine("Error: {0}", result.ErrorMessage);
                return -1;
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

        private static async Task<int> CompressDirectoryAsync(
            CompressionApplicationService applicationService,
            string directoryPath,
            CompressionSettings settings)
        {
            Console.WriteLine("Directory compression in progress (recursive)...");
            Console.WriteLine($"Target directory: {directoryPath}");

            var result = await applicationService.CompressDirectoryAsync(directoryPath, settings);

            if (result.HasGeneralError)
            {
                Console.WriteLine("Error: {0}", result.ErrorMessage);
                return -1;
            }

            Console.WriteLine("Processed files: {0}", result.TotalFiles);
            Console.WriteLine("Succeeded: {0}", result.SucceededFiles);
            Console.WriteLine("Failed: {0}", result.FailedFiles);

            if (!result.IsSuccess)
            {
                foreach (var failure in result.FileResults.Where(r => !r.IsSuccess))
                {
                    Console.WriteLine($"[FAILED] {failure.FilePath}: {failure.ErrorMessage}");
                }

                return -1;
            }

            Console.WriteLine("Directory compression completed successfully.");
            return 0;
        }

        private static async Task<int> CompressDirectoryToSingleArchiveAsync(
            CompressionApplicationService applicationService,
            string directoryPath,
            CompressionSettings settings,
            Stopwatch stopwatch)
        {
            Console.WriteLine("Compressing directory to single archive (tar.gz)...");
            Console.WriteLine($"Target directory: {directoryPath}");
            Console.Write("Press <P> to pause, <R> to resume or <X> to interrupt the compression process...\b\n");
            Console.Write("Archive compression in progress...\b\n");

            var result = await applicationService.CompressDirectoryToSingleArchiveAsync(directoryPath, null, settings);

            if (result.IsSuccess)
            {
                stopwatch.Stop();
                Console.WriteLine();
                Console.WriteLine($"Archive created: {result.Job.OutputFile.FullPath}");
                Console.WriteLine("Done in {0}s", stopwatch.Elapsed.TotalSeconds);
                return 0;
            }

            Console.WriteLine();
            Console.WriteLine("Error: {0}", result.ErrorMessage);
            return -1;
        }
    }
}
