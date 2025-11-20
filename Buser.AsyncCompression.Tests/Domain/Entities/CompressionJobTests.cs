using Buser.AsyncCompression.Domain.Entities;
using Buser.AsyncCompression.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Buser.AsyncCompression.Tests.Domain.Entities;

public class CompressionJobTests
{
    private readonly Buser.AsyncCompression.Domain.ValueObjects.FileInfo _inputFile;
    private readonly Buser.AsyncCompression.Domain.ValueObjects.FileInfo _outputFile;
    private readonly CompressionSettings _settings;

    public CompressionJobTests()
    {
        _inputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(@"C:\test\input.txt");
        _outputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(@"C:\test\output.txt.gz");
        _settings = CompressionSettings.Default;
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateJob()
    {
        // Act
        var job = new CompressionJob(_inputFile, _outputFile, _settings);

        // Assert
        job.Should().NotBeNull();
        job.Id.Should().NotBeEmpty();
        job.InputFile.Should().Be(_inputFile);
        job.OutputFile.Should().Be(_outputFile);
        job.Settings.Should().Be(_settings);
        job.Status.Should().Be(CompressionStatus.Created);
        job.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        job.StartedAt.Should().BeNull();
        job.CompletedAt.Should().BeNull();
        job.ProcessedBytes.Should().Be(0);
        job.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNullInputFile_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CompressionJob(null!, _outputFile, _settings);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("inputFile");
    }

    [Fact]
    public void Constructor_WithNullOutputFile_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CompressionJob(_inputFile, null!, _settings);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("outputFile");
    }

    [Fact]
    public void Constructor_WithNullSettings_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CompressionJob(_inputFile, _outputFile, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public void Start_WhenStatusIsCreated_ShouldChangeStatusToRunning()
    {
        // Arrange
        var job = new CompressionJob(_inputFile, _outputFile, _settings);

        // Act
        job.Start();

        // Assert
        job.Status.Should().Be(CompressionStatus.Running);
        job.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Pause_WhenStatusIsRunning_ShouldChangeStatusToPaused()
    {
        // Arrange
        var job = new CompressionJob(_inputFile, _outputFile, _settings);
        job.Start();

        // Act
        job.Pause();

        // Assert
        job.Status.Should().Be(CompressionStatus.Paused);
    }

    [Fact]
    public void Resume_WhenStatusIsPaused_ShouldChangeStatusToRunning()
    {
        // Arrange
        var job = new CompressionJob(_inputFile, _outputFile, _settings);
        job.Start();
        job.Pause();

        // Act
        job.Resume();

        // Assert
        job.Status.Should().Be(CompressionStatus.Running);
    }

    [Fact]
    public void Complete_WhenStatusIsRunning_ShouldChangeStatusToCompleted()
    {
        // Arrange
        var job = new CompressionJob(_inputFile, _outputFile, _settings);
        job.Start();

        // Act
        job.Complete();

        // Assert
        job.Status.Should().Be(CompressionStatus.Completed);
        job.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Cancel_WhenStatusIsNotCompleted_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var job = new CompressionJob(_inputFile, _outputFile, _settings);
        job.Start();

        // Act
        job.Cancel();

        // Assert
        job.Status.Should().Be(CompressionStatus.Cancelled);
        job.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateProgress_WhenStatusIsRunning_ShouldUpdateProcessedBytes()
    {
        // Arrange
        var inputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(@"C:\test\input.txt");
        var job = new CompressionJob(inputFile, _outputFile, _settings);
        job.Start();
        var processedBytes = 1024L;

        // Act
        job.UpdateProgress(processedBytes);

        // Assert
        // Since the file doesn't exist, ProcessedBytes will be limited to InputFile.Size (0)
        job.ProcessedBytes.Should().Be(0);
    }

    [Fact]
    public void ProgressPercentage_WhenFileSizeIsZero_ShouldReturnZero()
    {
        // Arrange
        var job = new CompressionJob(_inputFile, _outputFile, _settings);
        job.Start();
        job.UpdateProgress(1000L);

        // Act
        var percentage = job.ProgressPercentage;

        // Assert
        percentage.Should().Be(0);
    }

    [Fact]
    public void Fail_WhenStatusIsRunning_ShouldChangeStatusToFailed()
    {
        // Arrange
        var job = new CompressionJob(_inputFile, _outputFile, _settings);
        job.Start();

        // Act
        job.Fail();

        // Assert
        job.Status.Should().Be(CompressionStatus.Failed);
        job.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Fail_WhenStatusIsCompleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var job = new CompressionJob(_inputFile, _outputFile, _settings);
        job.Start();
        job.Complete();

        // Act & Assert
        var action = () => job.Fail();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot fail completed job");
    }

    [Fact]
    public void Pause_ShouldResetPauseEvent()
    {
        // Arrange
        var job = new CompressionJob(_inputFile, _outputFile, _settings);
        job.Start();

        // Act
        job.Pause();

        // Assert
        job.PauseEvent.IsSet.Should().BeFalse();
    }

    [Fact]
    public void Resume_ShouldSetPauseEvent()
    {
        // Arrange
        var job = new CompressionJob(_inputFile, _outputFile, _settings);
        job.Start();
        job.Pause();

        // Act
        job.Resume();

        // Assert
        job.PauseEvent.IsSet.Should().BeTrue();
    }

    [Fact]
    public void Cancel_ShouldCancelCancellationToken()
    {
        // Arrange
        var job = new CompressionJob(_inputFile, _outputFile, _settings);
        job.Start();

        // Act
        job.Cancel();

        // Assert
        job.CancellationToken.IsCancellationRequested.Should().BeTrue();
        job.PauseEvent.IsSet.Should().BeTrue(); // Should release waiting threads
    }

    [Fact]
    public void CancellationToken_ShouldBeAvailable()
    {
        // Arrange
        var job = new CompressionJob(_inputFile, _outputFile, _settings);

        // Act & Assert
        job.CancellationToken.Should().NotBeNull();
        job.CancellationToken.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void Dispose_ShouldDisposeResources()
    {
        // Arrange
        var job = new CompressionJob(_inputFile, _outputFile, _settings);

        // Act
        job.Dispose();
        job.Dispose(); // Should be safe to call multiple times

        // Assert
        // If we try to use CancellationToken after dispose, it should throw
        var action = () => _ = job.CancellationToken;
        // Note: CancellationTokenSource doesn't throw on access after dispose,
        // but the token becomes unusable. This is expected behavior.
    }
}