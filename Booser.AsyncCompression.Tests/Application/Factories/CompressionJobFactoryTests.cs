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
        var inputFilePath = @"C:\test\input.txt";

        // Act
        var job = _factory.CreateJob(inputFilePath);

        // Assert
        job.Should().NotBeNull();
        job.InputFile.FullPath.Should().Be(inputFilePath);
        job.OutputFile.FullPath.Should().Be(inputFilePath + ".gz");
        job.Settings.Should().Be(CompressionSettings.Default);
        job.Status.Should().Be(CompressionStatus.Created);
    }

    [Fact]
    public void CreateJob_WithCustomSettings_ShouldUseCustomSettings()
    {
        // Arrange
        var inputFilePath = @"C:\test\input.txt";
        var customSettings = new CompressionSettings(2048, 8192);

        // Act
        var job = _factory.CreateJob(inputFilePath, customSettings);

        // Assert
        job.Should().NotBeNull();
        job.Settings.Should().Be(customSettings);
        job.InputFile.FullPath.Should().Be(inputFilePath);
        job.OutputFile.FullPath.Should().Be(inputFilePath + ".gz");
    }

    [Fact]
    public void CreateJob_WithNullInputPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => _factory.CreateJob(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateJob_WithEmptyInputPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => _factory.CreateJob("");
        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(@"C:\test\file.txt", @"C:\test\file.txt.gz")]
    [InlineData(@"C:\test\document.pdf", @"C:\test\document.pdf.gz")]
    [InlineData(@"file.txt", @"file.txt.gz")]
    public void CreateJob_ShouldGenerateCorrectOutputPath(string inputPath, string expectedOutputPath)
    {
        // Act
        var job = _factory.CreateJob(inputPath);

        // Assert
        job.OutputFile.FullPath.Should().Be(expectedOutputPath);
    }

    [Fact]
    public void CreateJob_WithNullSettings_ShouldUseDefaultSettings()
    {
        // Arrange
        var inputFilePath = @"C:\test\input.txt";

        // Act
        var job = _factory.CreateJob(inputFilePath, null);

        // Assert
        job.Settings.Should().Be(CompressionSettings.Default);
    }

    [Fact]
    public void CreateJob_ShouldCreateUniqueJobIds()
    {
        // Arrange
        var inputFilePath = @"C:\test\input.txt";

        // Act
        var job1 = _factory.CreateJob(inputFilePath);
        var job2 = _factory.CreateJob(inputFilePath);

        // Assert
        job1.Id.Should().NotBe(job2.Id);
    }
}