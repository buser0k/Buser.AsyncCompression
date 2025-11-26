using System;
using System.IO;
using System.Threading.Tasks;
using System.IO.Compression;
using Buser.AsyncCompression.Application.Factories;
using Buser.AsyncCompression.Application.Services;
using Buser.AsyncCompression.Domain.Interfaces;
using Buser.AsyncCompression.Infrastructure.Algorithms;
using Buser.AsyncCompression.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Buser.AsyncCompression.Tests.Integration;

public class DirectoryCompressionIntegrationTests : IDisposable
{
    private readonly CompressionApplicationService _applicationService;
    private readonly string _rootDirectory;
    private readonly Mock<IProgressReporter> _progressReporter = new();
    private readonly Mock<ILogger<CompressionService>> _compressionLogger = new();
    private readonly Mock<ILogger<CompressionApplicationService>> _appLogger = new();

    public DirectoryCompressionIntegrationTests()
    {
        var fileService = new FileService();
        var algorithmFactory = new CompressionAlgorithmFactory();
        var defaultAlgorithm = new GZipCompressionAlgorithm();

        var compressionService = new CompressionService(
            defaultCompressionAlgorithm: defaultAlgorithm,
            fileService: fileService,
            progressReporter: _progressReporter.Object,
            logger: _compressionLogger.Object,
            algorithmFactory: algorithmFactory);

        var jobFactory = new CompressionJobFactory(defaultAlgorithm, algorithmFactory);
        _applicationService = new CompressionApplicationService(
            compressionService,
            fileService,
            jobFactory,
            _progressReporter.Object,
            _appLogger.Object);

        _rootDirectory = Path.Combine(Path.GetTempPath(), "AsyncCompressionTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_rootDirectory);
    }

    [Fact]
    public async Task CompressDirectoryAsync_ShouldCompressAllFilesRecursively()
    {
        var nested = Path.Combine(_rootDirectory, "nested");
        Directory.CreateDirectory(nested);

        var file1 = Path.Combine(_rootDirectory, "file1.txt");
        var file2 = Path.Combine(nested, "file2.log");

        File.WriteAllText(file1, "content-1");
        File.WriteAllText(file2, "content-2");

        var result = await _applicationService.CompressDirectoryAsync(_rootDirectory);

        result.IsSuccess.Should().BeTrue();
        result.TotalFiles.Should().Be(2);
        File.Exists(file1 + ".gz").Should().BeTrue();
        File.Exists(file2 + ".gz").Should().BeTrue();
    }

    [Fact]
    public async Task CompressDirectoryAsync_ShouldReturnEmptyResult_WhenNoFiles()
    {
        var directoryResult = await _applicationService.CompressDirectoryAsync(_rootDirectory);

        directoryResult.TotalFiles.Should().Be(0);
        directoryResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CompressDirectoryAsync_ShouldReturnError_WhenDirectoryMissing()
    {
        var missingDirectory = Path.Combine(_rootDirectory, "missing");

        var result = await _applicationService.CompressDirectoryAsync(missingDirectory);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("does not exist");
    }

    [Fact]
    public async Task CompressDirectoryToArchiveAsync_ShouldCreateZipWithStructure()
    {
        var nested = Path.Combine(_rootDirectory, "nested");
        Directory.CreateDirectory(nested);

        var file1 = Path.Combine(_rootDirectory, "file1.txt");
        var file2 = Path.Combine(nested, "file2.log");

        File.WriteAllText(file1, "content-1");
        File.WriteAllText(file2, "content-2");

        var archivePath = await _applicationService.CompressDirectoryToArchiveAsync(_rootDirectory);

        archivePath.Should().NotBeNullOrEmpty();
        File.Exists(archivePath!).Should().BeTrue();

        using var archive = ZipFile.OpenRead(archivePath!);
        var entries = archive.Entries.Select(e => e.FullName.Replace('\\', '/')).ToList();

        entries.Should().Contain("file1.txt.gz");
        entries.Should().Contain("nested/file2.log.gz");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_rootDirectory))
            {
                Directory.Delete(_rootDirectory, true);
            }
        }
        catch
        {
            // ignored
        }
    }
}

