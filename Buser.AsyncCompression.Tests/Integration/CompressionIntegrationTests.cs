using Buser.AsyncCompression.Application.Factories;
using Buser.AsyncCompression.Application.Services;
using Buser.AsyncCompression.Domain.Entities;
using Buser.AsyncCompression.Domain.Interfaces;
using Buser.AsyncCompression.Domain.ValueObjects;
using Buser.AsyncCompression.Infrastructure.Algorithms;
using Buser.AsyncCompression.Infrastructure.DI;
using Buser.AsyncCompression.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Buser.AsyncCompression.Tests.Integration;

public class CompressionIntegrationTests : IDisposable
{
    private readonly List<string> _tempFiles;
    private readonly ServiceProvider _serviceProvider;

    public CompressionIntegrationTests()
    {
        _tempFiles = new List<string>();
        _serviceProvider = ServiceConfiguration.ConfigureServices();
    }

    [Fact]
    public void ServiceConfiguration_ShouldRegisterAllRequiredServices()
    {
        // Act & Assert
        _serviceProvider.GetRequiredService<IFileService>().Should().NotBeNull();
        _serviceProvider.GetRequiredService<ICompressionAlgorithm>().Should().NotBeNull();
        _serviceProvider.GetRequiredService<CompressionJobFactory>().Should().NotBeNull();
        
        // Note: ICompressionService and CompressionApplicationService require IProgressReporter
        // which is only registered when provided to ConfigureServices
    }

    [Fact]
    public void ServiceConfiguration_WithProgressReporter_ShouldRegisterAllServices()
    {
        // Arrange
        var mockProgressReporter = new Moq.Mock<Buser.AsyncCompression.Domain.Interfaces.IProgressReporter>().Object;
        using var serviceProvider = ServiceConfiguration.ConfigureServices(mockProgressReporter);

        // Act & Assert
        serviceProvider.GetRequiredService<IFileService>().Should().NotBeNull();
        serviceProvider.GetRequiredService<ICompressionAlgorithm>().Should().NotBeNull();
        serviceProvider.GetRequiredService<ICompressionService>().Should().NotBeNull();
        serviceProvider.GetRequiredService<CompressionApplicationService>().Should().NotBeNull();
        serviceProvider.GetRequiredService<CompressionJobFactory>().Should().NotBeNull();
    }

    [Fact]
    public void CompressionJobFactory_ShouldCreateValidJob()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<CompressionJobFactory>();
        var inputPath = Path.Combine("test", "input.txt");

        // Act
        var job = factory.CreateJob(inputPath);

        // Assert
        job.Should().NotBeNull();
        job.InputFile.FullPath.Should().Be(Path.GetFullPath(inputPath));
        job.OutputFile.FullPath.Should().EndWith(".gz"); // Should use algorithm's extension
        job.Status.Should().Be(CompressionStatus.Created);
    }

    [Fact]
    public void GZipCompressionAlgorithm_ShouldCompressData()
    {
        // Arrange
        var algorithm = new GZipCompressionAlgorithm();
        var testData = System.Text.Encoding.UTF8.GetBytes("Test data for compression");

        // Act
        var compressedData = algorithm.Compress(testData);

        // Assert
        compressedData.Should().NotBeNull();
        compressedData.Length.Should().BeGreaterThan(0);
        // For small data, compression might not be effective
    }

    [Fact]
    public async Task FileService_ShouldHandleFileOperations()
    {
        // Arrange
        var fileService = new FileService();
        var testContent = "Test file content";
        var tempFile = CreateTempFile(testContent);
        var fileInfo = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(tempFile);

        // Act & Assert
        var exists = await fileService.ExistsAsync(fileInfo);
        exists.Should().BeTrue();

        using var stream = await fileService.OpenReadAsync(fileInfo);
        stream.Should().NotBeNull();
        stream.CanRead.Should().BeTrue();
    }

    private string CreateTempFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        _tempFiles.Add(tempFile);
        return tempFile;
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        
        foreach (var tempFile in _tempFiles)
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
