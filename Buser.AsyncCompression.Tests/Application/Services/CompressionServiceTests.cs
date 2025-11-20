using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Buser.AsyncCompression.Application.Factories;
using Buser.AsyncCompression.Application.Services;
using Buser.AsyncCompression.Domain.Entities;
using Buser.AsyncCompression.Domain.Interfaces;
using Buser.AsyncCompression.Domain.ValueObjects;
using Buser.AsyncCompression.Infrastructure.Algorithms;
using Buser.AsyncCompression.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Buser.AsyncCompression.Tests.Application.Services;

public class CompressionServiceTests : IDisposable
{
    private readonly Mock<IProgressReporter> _mockProgressReporter;
    private readonly ICompressionAlgorithm _compressionAlgorithm;
    private readonly IFileService _fileService;
    private readonly CompressionService _compressionService;
    private readonly string _tempInputFile;
    private readonly string _tempOutputFile;

    public CompressionServiceTests()
    {
        _mockProgressReporter = new Mock<IProgressReporter>();
        _compressionAlgorithm = new GZipCompressionAlgorithm();
        _fileService = new FileService();
        var mockLogger = new Mock<ILogger<CompressionService>>();
        var algorithmFactory = new CompressionAlgorithmFactory();
        _compressionService = new CompressionService(
            _compressionAlgorithm,
            _fileService,
            _mockProgressReporter.Object,
            mockLogger.Object,
            algorithmFactory);

        // Create temporary test file
        _tempInputFile = Path.GetTempFileName();
        _tempOutputFile = _tempInputFile + ".gz";
        File.WriteAllText(_tempInputFile, "Test content for compression");
    }

    [Fact]
    public void Constructor_WithNullAlgorithm_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CompressionService>>();
        var algorithmFactory = new CompressionAlgorithmFactory();

        // Act & Assert
        var action = () => new CompressionService(
            null!,
            _fileService,
            _mockProgressReporter.Object,
            mockLogger.Object,
            algorithmFactory);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("defaultCompressionAlgorithm");
    }

    [Fact]
    public void Constructor_WithNullFileService_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CompressionService>>();
        var algorithmFactory = new CompressionAlgorithmFactory();

        // Act & Assert
        var action = () => new CompressionService(
            _compressionAlgorithm,
            null!,
            _mockProgressReporter.Object,
            mockLogger.Object,
            algorithmFactory);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("fileService");
    }

    [Fact]
    public void Constructor_WithNullProgressReporter_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CompressionService>>();
        var algorithmFactory = new CompressionAlgorithmFactory();

        // Act & Assert
        var action = () => new CompressionService(
            _compressionAlgorithm,
            _fileService,
            null!,
            mockLogger.Object,
            algorithmFactory);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("progressReporter");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var algorithmFactory = new CompressionAlgorithmFactory();

        // Act & Assert
        var action = () => new CompressionService(
            _compressionAlgorithm,
            _fileService,
            _mockProgressReporter.Object,
            null!,
            algorithmFactory);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullAlgorithmFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CompressionService>>();

        // Act & Assert
        var action = () => new CompressionService(
            _compressionAlgorithm,
            _fileService,
            _mockProgressReporter.Object,
            mockLogger.Object,
            null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("algorithmFactory");
    }

    [Fact]
    public async Task CompressAsync_WithValidJob_ShouldCompleteSuccessfully()
    {
        // Arrange
        var inputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempInputFile);
        var outputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempOutputFile);
        var settings = CompressionSettings.Default;
        var job = new CompressionJob(inputFile, outputFile, settings);

        // Act
        var result = await _compressionService.CompressAsync(job);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(CompressionStatus.Completed);
        result.StartedAt.Should().NotBeNull();
        result.CompletedAt.Should().NotBeNull();
        File.Exists(_tempOutputFile).Should().BeTrue();
        _mockProgressReporter.Verify(x => x.Report(It.IsAny<double>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Pause_WhenJobIsRunning_ShouldPauseJob()
    {
        // Arrange
        var inputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempInputFile);
        var outputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempOutputFile);
        var settings = CompressionSettings.Default;
        var job = new CompressionJob(inputFile, outputFile, settings);
        job.Start();

        // Act
        _compressionService.Pause(job);

        // Assert
        job.Status.Should().Be(CompressionStatus.Paused);
        job.PauseEvent.IsSet.Should().BeFalse();
    }

    [Fact]
    public void Pause_WhenJobIsNotRunning_ShouldNotChangeStatus()
    {
        // Arrange
        var inputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempInputFile);
        var outputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempOutputFile);
        var settings = CompressionSettings.Default;
        var job = new CompressionJob(inputFile, outputFile, settings);
        // Job is in Created status

        // Act
        _compressionService.Pause(job);

        // Assert
        job.Status.Should().Be(CompressionStatus.Created);
    }

    [Fact]
    public void Resume_WhenJobIsPaused_ShouldResumeJob()
    {
        // Arrange
        var inputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempInputFile);
        var outputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempOutputFile);
        var settings = CompressionSettings.Default;
        var job = new CompressionJob(inputFile, outputFile, settings);
        job.Start();
        job.Pause();

        // Act
        _compressionService.Resume(job);

        // Assert
        job.Status.Should().Be(CompressionStatus.Running);
        job.PauseEvent.IsSet.Should().BeTrue();
    }

    [Fact]
    public void Resume_WhenJobIsNotPaused_ShouldNotChangeStatus()
    {
        // Arrange
        var inputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempInputFile);
        var outputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempOutputFile);
        var settings = CompressionSettings.Default;
        var job = new CompressionJob(inputFile, outputFile, settings);
        job.Start();
        // Job is in Running status

        // Act
        _compressionService.Resume(job);

        // Assert
        job.Status.Should().Be(CompressionStatus.Running);
    }

    [Fact]
    public void Cancel_WhenJobIsRunning_ShouldCancelJob()
    {
        // Arrange
        var inputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempInputFile);
        var outputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempOutputFile);
        var settings = CompressionSettings.Default;
        var job = new CompressionJob(inputFile, outputFile, settings);
        job.Start();

        // Act
        _compressionService.Cancel(job);

        // Assert
        job.Status.Should().Be(CompressionStatus.Cancelled);
        job.CancellationToken.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public void CompressAsync_WithCancelledJob_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var inputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempInputFile);
        var outputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempOutputFile);
        var settings = CompressionSettings.Default;
        var job = new CompressionJob(inputFile, outputFile, settings);
        job.Start();
        job.Cancel();

        // Act & Assert
        var action = async () => await _compressionService.CompressAsync(job);
        action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot start job in Cancelled status");
    }

    [Fact]
    public void CompressAsync_MultipleJobs_ShouldNotInterfereWithEachOther()
    {
        // Arrange
        var tempFile1 = Path.GetTempFileName();
        var tempFile2 = Path.GetTempFileName();
        File.WriteAllText(tempFile1, "Test content 1");
        File.WriteAllText(tempFile2, "Test content 2");

        var inputFile1 = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(tempFile1);
        var outputFile1 = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(tempFile1 + ".gz");
        var job1 = new CompressionJob(inputFile1, outputFile1, CompressionSettings.Default);

        var inputFile2 = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(tempFile2);
        var outputFile2 = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(tempFile2 + ".gz");
        var job2 = new CompressionJob(inputFile2, outputFile2, CompressionSettings.Default);

        // Act - Pause job1, but job2 should continue
        job1.Start();
        job2.Start();
        _compressionService.Pause(job1);

        // Assert
        job1.Status.Should().Be(CompressionStatus.Paused);
        job2.Status.Should().Be(CompressionStatus.Running);
        job1.PauseEvent.IsSet.Should().BeFalse();
        job2.PauseEvent.IsSet.Should().BeTrue(); // Should not be affected

        // Cleanup
        job1.Dispose();
        job2.Dispose();
        try { File.Delete(tempFile1); } catch { }
        try { File.Delete(tempFile2); } catch { }
        try { File.Delete(tempFile1 + ".gz"); } catch { }
        try { File.Delete(tempFile2 + ".gz"); } catch { }
    }

    [Fact]
    public async Task CompressAsync_WithBrotliOutputFile_ShouldUseBrotliAlgorithm()
    {
        // Arrange
        var inputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempInputFile);
        var outputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(_tempInputFile + ".br");
        var settings = CompressionSettings.Default;
        var job = new CompressionJob(inputFile, outputFile, settings);

        // Act
        var result = await _compressionService.CompressAsync(job);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(CompressionStatus.Completed);
        File.Exists(_tempInputFile + ".br").Should().BeTrue();
        
        // Cleanup
        try { File.Delete(_tempInputFile + ".br"); } catch { }
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempInputFile))
                File.Delete(_tempInputFile);
            if (File.Exists(_tempOutputFile))
                File.Delete(_tempOutputFile);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

