using Booser.AsyncCompression.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Booser.AsyncCompression.Tests.Domain.ValueObjects;

public class CompressionSettingsTests
{
    [Fact]
    public void Default_ShouldReturnDefaultSettings()
    {
        // Act
        var settings = CompressionSettings.Default;

        // Assert
        settings.Should().NotBeNull();
        settings.BufferSize.Should().BeGreaterThan(0);
        settings.MaxBufferSize.Should().BeGreaterThan(0);
        settings.MaxBufferSize.Should().BeGreaterThanOrEqualTo(settings.BufferSize);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateSettings()
    {
        // Arrange
        var bufferSize = 1024;
        var maxBufferSize = 4096;

        // Act
        var settings = new CompressionSettings(bufferSize, maxBufferSize);

        // Assert
        settings.BufferSize.Should().Be(bufferSize);
        settings.MaxBufferSize.Should().Be(maxBufferSize);
    }

    [Fact]
    public void Constructor_WithZeroBufferSize_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => new CompressionSettings(0, 4096);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNegativeBufferSize_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => new CompressionSettings(-1, 4096);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithMaxBufferSizeLessThanBufferSize_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => new CompressionSettings(4096, 1024);
        action.Should().Throw<ArgumentException>();
    }
}