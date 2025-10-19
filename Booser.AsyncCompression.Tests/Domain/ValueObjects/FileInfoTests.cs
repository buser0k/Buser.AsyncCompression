using Booser.AsyncCompression.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Booser.AsyncCompression.Tests.Domain.ValueObjects;

public class FileInfoTests
{
    [Fact]
    public void Constructor_WithValidPath_ShouldCreateFileInfo()
    {
        // Arrange
        var filePath = Path.Combine("test", "file.txt");

        // Act
        var fileInfo = new Booser.AsyncCompression.Domain.ValueObjects.FileInfo(filePath);

        // Assert
        fileInfo.FullPath.Should().Be(Path.GetFullPath(filePath));
        fileInfo.Name.Should().Be("file.txt");
        // Note: Extension and Directory properties are not available in the current implementation
    }

    [Fact]
    public void Constructor_WithNullPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => new Booser.AsyncCompression.Domain.ValueObjects.FileInfo(null!);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => new Booser.AsyncCompression.Domain.ValueObjects.FileInfo("");
        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("test/file.txt", "file.txt")]
    [InlineData("test/subfolder/document.pdf", "document.pdf")]
    [InlineData("file.txt", "file.txt")]
    public void Name_ShouldExtractCorrectFileName(string filePath, string expectedName)
    {
        // Act
        var fileInfo = new Booser.AsyncCompression.Domain.ValueObjects.FileInfo(filePath);

        // Assert
        fileInfo.Name.Should().Be(expectedName);
    }

    // Note: Extension property is not available in the current implementation

    [Fact]
    public void ToString_ShouldReturnFullPath()
    {
        // Arrange
        var filePath = Path.Combine("test", "file.txt");
        var fileInfo = new Booser.AsyncCompression.Domain.ValueObjects.FileInfo(filePath);

        // Act
        var result = fileInfo.ToString();

        // Assert
        result.Should().Be(Path.GetFullPath(filePath));
    }
}