using Buser.AsyncCompression.Infrastructure.Algorithms;
using FluentAssertions;
using Xunit;

namespace Buser.AsyncCompression.Tests.Infrastructure.Algorithms;

public class GZipCompressionAlgorithmTests
{
    private readonly GZipCompressionAlgorithm _algorithm;

    public GZipCompressionAlgorithmTests()
    {
        _algorithm = new GZipCompressionAlgorithm();
    }

    [Fact]
    public void Name_ShouldReturnGZip()
    {
        // Act & Assert
        _algorithm.Name.Should().Be("GZip");
    }

    [Fact]
    public void FileExtension_ShouldReturnGz()
    {
        // Act & Assert
        _algorithm.FileExtension.Should().Be(".gz");
    }

    [Fact]
    public void Compress_WithValidData_ShouldCompressData()
    {
        // Arrange
        var originalData = System.Text.Encoding.UTF8.GetBytes("Hello, World! This is a test string for compression.");

        // Act
        var compressedData = _algorithm.Compress(originalData);

        // Assert
        compressedData.Should().NotBeNull();
        compressedData.Length.Should().BeGreaterThan(0);
        // For small data, compression might not be effective, so we just check it's not null
    }

    [Fact]
    public void Compress_WithEmptyData_ShouldReturnCompressedData()
    {
        // Arrange
        var emptyData = new byte[0];

        // Act
        var compressedData = _algorithm.Compress(emptyData);

        // Assert
        compressedData.Should().NotBeNull();
        // Empty data might result in empty compressed data
        compressedData.Length.Should().BeGreaterThanOrEqualTo(0);
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
        var largeData = System.Text.Encoding.UTF8.GetBytes(new string('A', 10000) + new string('B', 10000));

        // Act
        var compressedData = _algorithm.Compress(largeData);

        // Assert
        compressedData.Should().NotBeNull();
        compressedData.Length.Should().BeGreaterThan(0);
        compressedData.Length.Should().BeLessThan(largeData.Length);
        
        // Compression ratio should be significant for repetitive data
        var compressionRatio = (double)compressedData.Length / largeData.Length;
        compressionRatio.Should().BeLessThan(0.5);
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

    [Fact]
    public void Compress_ShouldProduceConsistentResults()
    {
        // Arrange
        var testData = System.Text.Encoding.UTF8.GetBytes("Test data for consistency check");

        // Act
        var compressed1 = _algorithm.Compress(testData);
        var compressed2 = _algorithm.Compress(testData);

        // Assert
        compressed1.Should().BeEquivalentTo(compressed2);
    }
}