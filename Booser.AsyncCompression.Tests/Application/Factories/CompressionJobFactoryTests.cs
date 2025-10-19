using Booser.AsyncCompression.Application.Factories;
using Booser.AsyncCompression.Domain.Entities;
using Booser.AsyncCompression.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Booser.AsyncCompression.Tests.Application.Factories;

public class CompressionJobFactoryTests
{
    private readonly CompressionJobFactory _factory;

    public CompressionJobFactoryTests()
    {
        _factory = new CompressionJobFactory();
    }

    [Fact]
    public void CreateJob_WithValidInputPath_ShouldCreateJob()
    {
        // Arrange
        var inputFilePath = Path.Combine("test", "input.txt");

        // Act
        var job = _factory.CreateJob(inputFilePath);

        // Assert
        job.Should().NotBeNull();
        job.InputFile.FullPath.Should().Be(Path.GetFullPath(inputFilePath));
        job.OutputFile.FullPath.Should().EndWith(".gz");
        job.Settings.BufferSize.Should().Be(CompressionSettings.Default.BufferSize);
        job.Settings.MaxBufferSize.Should().Be(CompressionSettings.Default.MaxBufferSize);
        job.Status.Should().Be(CompressionStatus.Created);
    }

    [Fact]
    public void CreateJob_WithCustomSettings_ShouldUseCustomSettings()
    {
        // Arrange
        var inputFilePath = Path.Combine("test", "input.txt");
        var customSettings = new CompressionSettings(2048, 8192);

        // Act
        var job = _factory.CreateJob(inputFilePath, customSettings);

        // Assert
        job.Should().NotBeNull();
        job.Settings.Should().Be(customSettings);
        job.InputFile.FullPath.Should().Be(Path.GetFullPath(inputFilePath));
        job.OutputFile.FullPath.Should().Be(Path.GetFullPath(inputFilePath) + ".gz");
    }

    [Fact]
    public void CreateJob_WithNullInputPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => _factory.CreateJob(null!);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateJob_WithEmptyInputPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => _factory.CreateJob("");
        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("test/file.txt")]
    [InlineData("test/document.pdf")]
    [InlineData("file.txt")]
    public void CreateJob_ShouldGenerateCorrectOutputPath(string inputPath)
    {
        // Act
        var job = _factory.CreateJob(inputPath);

        // Assert
        // Note: FullPath returns absolute path, so we check that it ends with expected extension
        job.OutputFile.FullPath.Should().EndWith(".gz");
        job.OutputFile.FullPath.Should().Contain(Path.GetFileNameWithoutExtension(inputPath));
    }

    [Fact]
    public void CreateJob_WithNullSettings_ShouldUseDefaultSettings()
    {
        // Arrange
        var inputFilePath = Path.Combine("test", "input.txt");

        // Act
        var job = _factory.CreateJob(inputFilePath, null);

        // Assert
        job.Settings.BufferSize.Should().Be(CompressionSettings.Default.BufferSize);
        job.Settings.MaxBufferSize.Should().Be(CompressionSettings.Default.MaxBufferSize);
        job.Settings.BoundedCapacity.Should().Be(CompressionSettings.Default.BoundedCapacity);
        job.Settings.MaxDegreeOfParallelism.Should().Be(CompressionSettings.Default.MaxDegreeOfParallelism);
    }

    [Fact]
    public void CreateJob_ShouldCreateUniqueJobIds()
    {
        // Arrange
        var inputFilePath = Path.Combine("test", "input.txt");

        // Act
        var job1 = _factory.CreateJob(inputFilePath);
        var job2 = _factory.CreateJob(inputFilePath);

        // Assert
        job1.Id.Should().NotBe(job2.Id);
    }
}