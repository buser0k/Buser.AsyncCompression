using Booser.AsyncCompression.Application.Factories;
using Booser.AsyncCompression.Domain.Interfaces;
using FluentAssertions;
using Xunit;

namespace Booser.AsyncCompression.Tests.Application.Factories;

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
}