using Buser.AsyncCompression.Domain.ValueObjects;
using Buser.AsyncCompression.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace Buser.AsyncCompression.Tests.Infrastructure.Services;

public class FileServiceTests : IDisposable
{
    private readonly FileService _service;
    private readonly List<string> _tempFiles;

    public FileServiceTests()
    {
        _service = new FileService();
        _tempFiles = new List<string>();
    }

    [Fact]
    public async Task OpenReadAsync_WithExistingFile_ShouldReturnStream()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        var fileInfo = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(tempFile);

        // Act
        using var stream = await _service.OpenReadAsync(fileInfo);

        // Assert
        stream.Should().NotBeNull();
        stream.CanRead.Should().BeTrue();
    }

    [Fact]
    public async Task OpenReadAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = @"C:\non\existent\file.txt";
        var fileInfo = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(nonExistentFile);

        // Act & Assert
        var action = async () => await _service.OpenReadAsync(fileInfo);
        await action.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnWritableStream()
    {
        // Arrange
        var tempFile = GetTempFilePath();
        var fileInfo = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(tempFile);

        // Act
        using var stream = await _service.CreateAsync(fileInfo);

        // Assert
        stream.Should().NotBeNull();
        stream.CanWrite.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingFile_ShouldReturnTrue()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        var fileInfo = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(tempFile);

        // Act
        var exists = await _service.ExistsAsync(fileInfo);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentFile = @"C:\non\existent\file.txt";
        var fileInfo = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(nonExistentFile);

        // Act
        var exists = await _service.ExistsAsync(fileInfo);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingFile_ShouldDeleteFile()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        var fileInfo = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(tempFile);

        // Act
        await _service.DeleteAsync(fileInfo);

        // Assert
        File.Exists(tempFile).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentFile_ShouldNotThrow()
    {
        // Arrange
        var nonExistentFile = @"C:\non\existent\file.txt";
        var fileInfo = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(nonExistentFile);

        // Act & Assert
        var action = async () => await _service.DeleteAsync(fileInfo);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OpenReadAsync_ShouldReturnAsyncStream()
    {
        // Arrange
        var tempFile = CreateTempFile("test content for async stream");
        var fileInfo = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(tempFile);

        // Act
        using var stream = await _service.OpenReadAsync(fileInfo);

        // Assert
        stream.Should().NotBeNull();
        stream.Should().BeAssignableTo<FileStream>();
        var fileStream = stream as FileStream;
        fileStream!.IsAsync.Should().BeTrue(); // Verify async mode is enabled
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnAsyncStream()
    {
        // Arrange
        var tempFile = GetTempFilePath();
        var fileInfo = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(tempFile);

        // Act
        using var stream = await _service.CreateAsync(fileInfo);

        // Assert
        stream.Should().NotBeNull();
        stream.Should().BeAssignableTo<FileStream>();
        var fileStream = stream as FileStream;
        fileStream!.IsAsync.Should().BeTrue(); // Verify async mode is enabled
    }

    [Fact]
    public async Task OpenReadAsync_ShouldNotBlockThread()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        var fileInfo = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(tempFile);
        var startTime = DateTime.UtcNow;
        var isAsync = false;

        // Act
        var task = _service.OpenReadAsync(fileInfo);
        isAsync = DateTime.UtcNow - startTime < TimeSpan.FromMilliseconds(100); // Should return immediately
        using var stream = await task;

        // Assert
        isAsync.Should().BeTrue("Async method should return immediately without blocking");
        stream.Should().NotBeNull();
    }

    private string CreateTempFile(string content)
    {
        var tempFile = GetTempFilePath();
        File.WriteAllText(tempFile, content);
        _tempFiles.Add(tempFile);
        return tempFile;
    }

    private string GetTempFilePath()
    {
        var tempFile = Path.GetTempFileName();
        _tempFiles.Add(tempFile);
        return tempFile;
    }

    public void Dispose()
    {
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