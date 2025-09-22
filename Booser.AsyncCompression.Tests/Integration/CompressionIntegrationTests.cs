using Booser.AsyncCompression.Application.Factories;
using Booser.AsyncCompression.Domain.Entities;
using Booser.AsyncCompression.Domain.Interfaces;
using Booser.AsyncCompression.Domain.ValueObjects;
using Booser.AsyncCompression.Infrastructure.Algorithms;
using Booser.AsyncCompression.Infrastructure.DI;
using Booser.AsyncCompression.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booser.AsyncCompression.Tests.Integration;

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
        _serviceProvider.GetRequiredService<ICompressionService>().Should().NotBeNull();
        _serviceProvider.GetRequiredService<IFileService>().Should().NotBeNull();
        _serviceProvider.GetRequiredService<ICompressionAlgorithm>().Should().NotBeNull();
        // Note: CompressionApplicationService is not registered in DI container
    }

    [Fact]
    public void CompressionJobFactory_ShouldCreateValidJob()
    {
        // Arrange
        var factory = new CompressionJobFactory();
        var inputPath = @"C:\test\input.txt";

        // Act
        var job = factory.CreateJob(inputPath);

        // Assert
        job.Should().NotBeNull();
        job.InputFile.FullPath.Should().Be(inputPath);
        job.OutputFile.FullPath.Should().Be(inputPath + ".gz");
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
        compressedData.Length.Should().BeLessThan(testData.Length);
    }

    [Fact]
    public async Task FileService_ShouldHandleFileOperations()
    {
        // Arrange
        var fileService = new FileService();
        var testContent = "Test file content";
        var tempFile = CreateTempFile(testContent);
        var fileInfo = new Booser.AsyncCompression.Domain.ValueObjects.FileInfo(tempFile);

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
