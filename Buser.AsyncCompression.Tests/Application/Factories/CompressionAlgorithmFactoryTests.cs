using Buser.AsyncCompression.Application.Factories;
using Buser.AsyncCompression.Domain.Interfaces;
using FluentAssertions;
using Xunit;

namespace Buser.AsyncCompression.Tests.Application.Factories;

public class CompressionAlgorithmFactoryTests
{
    private readonly CompressionAlgorithmFactory _factory;

    public CompressionAlgorithmFactoryTests()
    {
        _factory = new CompressionAlgorithmFactory();
    }

    [Fact]
    public void CreateGZipAlgorithm_ShouldReturnGZipAlgorithm()
    {
        // Act
        var algorithm = _factory.CreateGZipAlgorithm();

        // Assert
        algorithm.Should().NotBeNull();
        algorithm.Should().BeAssignableTo<ICompressionAlgorithm>();
        algorithm.Name.Should().Be("GZip");
        algorithm.FileExtension.Should().Be(".gz");
    }

    [Fact]
    public void CreateGZipAlgorithm_ShouldReturnNewInstanceEachTime()
    {
        // Act
        var algorithm1 = _factory.CreateGZipAlgorithm();
        var algorithm2 = _factory.CreateGZipAlgorithm();

        // Assert
        algorithm1.Should().NotBeSameAs(algorithm2);
        algorithm1.Should().BeOfType(algorithm2.GetType());
    }

    [Fact]
    public void CreateBrotliAlgorithm_ShouldReturnBrotliAlgorithm()
    {
        // Act
        var algorithm = _factory.CreateBrotliAlgorithm();

        // Assert
        algorithm.Should().NotBeNull();
        algorithm.Should().BeAssignableTo<ICompressionAlgorithm>();
        algorithm.Name.Should().Be("Brotli");
        algorithm.FileExtension.Should().Be(".br");
    }

    [Fact]
    public void CreateAlgorithm_WithGZipName_ShouldReturnGZipAlgorithm()
    {
        // Act
        var algorithm = _factory.CreateAlgorithm("gzip");

        // Assert
        algorithm.Should().NotBeNull();
        algorithm.Name.Should().Be("GZip");
        algorithm.FileExtension.Should().Be(".gz");
    }

    [Fact]
    public void CreateAlgorithm_WithBrotliName_ShouldReturnBrotliAlgorithm()
    {
        // Act
        var algorithm = _factory.CreateAlgorithm("brotli");

        // Assert
        algorithm.Should().NotBeNull();
        algorithm.Name.Should().Be("Brotli");
        algorithm.FileExtension.Should().Be(".br");
    }

    [Fact]
    public void CreateAlgorithm_WithBrAlias_ShouldReturnBrotliAlgorithm()
    {
        // Act
        var algorithm = _factory.CreateAlgorithm("br");

        // Assert
        algorithm.Should().NotBeNull();
        algorithm.Name.Should().Be("Brotli");
        algorithm.FileExtension.Should().Be(".br");
    }

    [Fact]
    public void CreateAlgorithm_WithUnknownName_ShouldReturnDefaultGZipAlgorithm()
    {
        // Act
        var algorithm = _factory.CreateAlgorithm("unknown");

        // Assert
        algorithm.Should().NotBeNull();
        algorithm.Name.Should().Be("GZip"); // Should default to GZip
        algorithm.FileExtension.Should().Be(".gz");
    }

    [Fact]
    public void CreateAlgorithm_WithNullName_ShouldReturnDefaultGZipAlgorithm()
    {
        // Act
        var algorithm = _factory.CreateAlgorithm(null!);

        // Assert
        algorithm.Should().NotBeNull();
        algorithm.Name.Should().Be("GZip");
        algorithm.FileExtension.Should().Be(".gz");
    }

    [Fact]
    public void CreateAlgorithm_WithEmptyName_ShouldReturnDefaultGZipAlgorithm()
    {
        // Act
        var algorithm = _factory.CreateAlgorithm("");

        // Assert
        algorithm.Should().NotBeNull();
        algorithm.Name.Should().Be("GZip");
        algorithm.FileExtension.Should().Be(".gz");
    }

    [Fact]
    public void CreateDefaultAlgorithm_ShouldReturnGZipAlgorithm()
    {
        // Act
        var algorithm = _factory.CreateDefaultAlgorithm();

        // Assert
        algorithm.Should().NotBeNull();
        algorithm.Name.Should().Be("GZip");
        algorithm.FileExtension.Should().Be(".gz");
    }
}