using Buser.AsyncCompression.Domain.Interfaces;
using Buser.AsyncCompression.Infrastructure.Algorithms;
using FluentAssertions;
using Xunit;

namespace Buser.AsyncCompression.Tests.Infrastructure.Algorithms;

public class BrotliCompressionAlgorithmTests
{
    private readonly BrotliCompressionAlgorithm _algorithm;

    public BrotliCompressionAlgorithmTests()
    {
        _algorithm = new BrotliCompressionAlgorithm();
    }

    [Fact]
    public void Name_ShouldReturnBrotli()
    {
        // Act & Assert
        _algorithm.Name.Should().Be("Brotli");
    }

    [Fact]
    public void FileExtension_ShouldReturnBr()
    {
        // Act & Assert
        _algorithm.FileExtension.Should().Be(".br");
    }

    [Fact]
    public void Compress_WithValidData_ShouldCompressData()
    {
        // Arrange
        var testData = System.Text.Encoding.UTF8.GetBytes("Test data for Brotli compression");

        // Act
        var compressedData = _algorithm.Compress(testData);

        // Assert
        compressedData.Should().NotBeNull();
        compressedData.Length.Should().BeGreaterThan(0);
        // Note: For small data, compression overhead might make result larger than original
    }

    [Fact]
    public void Compress_WithEmptyData_ShouldReturnCompressedData()
    {
        // Arrange
        var testData = System.Array.Empty<byte>();

        // Act
        var compressedData = _algorithm.Compress(testData);

        // Assert
        compressedData.Should().NotBeNull();
        compressedData.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compress_WithNullData_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => _algorithm.Compress(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Compress_WithLargeData_ShouldCompressEfficiently()
    {
        // Arrange
        // Use repetitive data that compresses well
        var pattern = System.Text.Encoding.UTF8.GetBytes("This is a repetitive pattern that should compress well. ");
        var largeData = new byte[1024 * 1024]; // 1MB
        for (int i = 0; i < largeData.Length; i += pattern.Length)
        {
            Array.Copy(pattern, 0, largeData, i, Math.Min(pattern.Length, largeData.Length - i));
        }

        // Act
        var compressedData = _algorithm.Compress(largeData);

        // Assert
        compressedData.Should().NotBeNull();
        compressedData.Length.Should().BeLessThan(largeData.Length); // Should compress repetitive data
    }

    [Fact]
    public void Compress_ShouldProduceConsistentResults()
    {
        // Arrange
        var testData = System.Text.Encoding.UTF8.GetBytes("Test data for consistent compression");

        // Act
        var compressed1 = _algorithm.Compress(testData);
        var compressed2 = _algorithm.Compress(testData);

        // Assert
        compressed1.Should().BeEquivalentTo(compressed2);
    }

    [Fact]
    public void Compress_WithBinaryData_ShouldCompressCorrectly()
    {
        // Arrange
        var binaryData = new byte[] { 0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE, 0xFD, 0xFC };

        // Act
        var compressedData = _algorithm.Compress(binaryData);

        // Assert
        compressedData.Should().NotBeNull();
        compressedData.Length.Should().BeGreaterThan(0);
    }
}

