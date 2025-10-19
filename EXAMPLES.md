# Примеры использования Buser.AsyncCompression

## 🚀 Базовые примеры

### 1. Простое сжатие файла

```bash
# Сжатие одного файла
dotnet run test.txt

# Результат: создается файл test.txt.gz
```

### 2. Сжатие с пользовательскими настройками

```csharp
// Program.cs - пример с пользовательскими настройками
var settings = new CompressionSettings(
    bufferSize: 16384,           // 16KB буфер
    maxBufferSize: 8388608,      // 8MB максимальный буфер
    boundedCapacity: 150,        // Увеличенная емкость
    maxDegreeOfParallelism: 4    // 4 потока
);

var result = await applicationService.CompressFileAsync("large_file.dat", settings);
```

### 3. Пакетное сжатие файлов

```csharp
// Пример пакетного сжатия
public async Task CompressMultipleFiles(string[] filePaths)
{
    var tasks = filePaths.Select(async filePath =>
    {
        try
        {
            var result = await applicationService.CompressFileAsync(filePath);
            Console.WriteLine($"File {filePath}: {(result.IsSuccess ? "Success" : result.ErrorMessage)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error compressing {filePath}: {ex.Message}");
        }
    });
    
    await Task.WhenAll(tasks);
}
```

## 🔧 Расширенные примеры

### 1. Создание собственного алгоритма сжатия

```csharp
// Реализация LZ4 алгоритма сжатия
public class LZ4CompressionAlgorithm : ICompressionAlgorithm
{
    public string Name => "LZ4";
    public string FileExtension => ".lz4";

    public byte[] Compress(byte[] bytes)
    {
        // Использование библиотеки LZ4
        return LZ4Codec.Encode(bytes, 0, bytes.Length);
    }
}

// Регистрация в DI контейнере
services.AddSingleton<ICompressionAlgorithm, LZ4CompressionAlgorithm>();
```

### 2. Создание собственного сервиса файлов

```csharp
// Реализация для работы с облачным хранилищем
public class CloudFileService : IFileService
{
    private readonly ICloudStorageClient _cloudClient;

    public CloudFileService(ICloudStorageClient cloudClient)
    {
        _cloudClient = cloudClient;
    }

    public async Task<Stream> OpenReadAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file)
    {
        return await _cloudClient.DownloadStreamAsync(file.FullPath);
    }

    public async Task<Stream> CreateAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file)
    {
        return new CloudUploadStream(_cloudClient, file.FullPath);
    }

    public async Task<bool> ExistsAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file)
    {
        return await _cloudClient.ExistsAsync(file.FullPath);
    }

    public async Task DeleteAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file)
    {
        await _cloudClient.DeleteAsync(file.FullPath);
    }
}
```

### 3. Создание собственного отчета о прогрессе

```csharp
// Реализация для отправки прогресса через WebSocket
public class WebSocketProgressReporter : IProgressReporter
{
    private readonly IWebSocketClient _webSocketClient;

    public WebSocketProgressReporter(IWebSocketClient webSocketClient)
    {
        _webSocketClient = webSocketClient;
    }

    public void Report(double progress)
    {
        var message = new
        {
            Type = "ProgressUpdate",
            Progress = progress,
            Timestamp = DateTime.UtcNow
        };

        _webSocketClient.SendAsync(JsonSerializer.Serialize(message));
    }
}
```

## 🎯 Практические сценарии

### 1. Сжатие логов сервера

```csharp
// Сжатие логов с ротацией
public class LogCompressionService
{
    private readonly CompressionApplicationService _compressionService;

    public async Task CompressOldLogs(string logDirectory)
    {
        var logFiles = Directory.GetFiles(logDirectory, "*.log")
            .Where(file => File.GetCreationTime(file) < DateTime.Now.AddDays(-7))
            .ToArray();

        foreach (var logFile in logFiles)
        {
            var settings = new CompressionSettings(
                bufferSize: 32768,  // 32KB для текстовых файлов
                maxDegreeOfParallelism: 2
            );

            var result = await _compressionService.CompressFileAsync(logFile, settings);
            
            if (result.IsSuccess)
            {
                File.Delete(logFile); // Удаляем оригинальный файл
                Console.WriteLine($"Compressed and deleted: {logFile}");
            }
        }
    }
}
```

### 2. Сжатие резервных копий

```csharp
// Сжатие резервных копий базы данных
public class BackupCompressionService
{
    public async Task CompressDatabaseBackup(string backupPath)
    {
        var settings = new CompressionSettings(
            bufferSize: 65536,      // 64KB для больших файлов
            maxBufferSize: 16777216, // 16MB максимальный буфер
            boundedCapacity: 50,    // Меньше параллелизма для больших файлов
            maxDegreeOfParallelism: Environment.ProcessorCount
        );

        var result = await _compressionService.CompressFileAsync(backupPath, settings);
        
        if (result.IsSuccess)
        {
            // Перемещаем сжатый файл в архив
            var compressedFile = backupPath + ".gz";
            var archivePath = Path.Combine("archive", Path.GetFileName(compressedFile));
            File.Move(compressedFile, archivePath);
        }
    }
}
```

### 3. Сжатие медиафайлов

```csharp
// Сжатие изображений и видео
public class MediaCompressionService
{
    public async Task CompressMediaFiles(string mediaDirectory)
    {
        var mediaExtensions = new[] { ".jpg", ".png", ".mp4", ".avi", ".mov" };
        
        var mediaFiles = Directory.GetFiles(mediaDirectory, "*.*", SearchOption.AllDirectories)
            .Where(file => mediaExtensions.Contains(Path.GetExtension(file).ToLower()))
            .ToArray();

        var settings = new CompressionSettings(
            bufferSize: 131072,     // 128KB для медиафайлов
            maxDegreeOfParallelism: Environment.ProcessorCount / 2 // Меньше нагрузки на CPU
        );

        var compressionTasks = mediaFiles.Select(async file =>
        {
            var result = await _compressionService.CompressFileAsync(file, settings);
            return new { File = file, Result = result };
        });

        var results = await Task.WhenAll(compressionTasks);
        
        foreach (var result in results)
        {
            Console.WriteLine($"{result.File}: {(result.Result.IsSuccess ? "Compressed" : result.Result.ErrorMessage)}");
        }
    }
}
```

## 🔄 Интеграция с другими системами

### 1. Интеграция с ASP.NET Core

```csharp
// Startup.cs или Program.cs
public void ConfigureServices(IServiceCollection services)
{
    // Регистрация сервисов сжатия
    services.AddSingleton<IFileService, FileService>();
    services.AddSingleton<ICompressionAlgorithm, GZipCompressionAlgorithm>();
    services.AddTransient<ICompressionService, CompressionService>();
    services.AddSingleton<CompressionJobFactory>();
    services.AddTransient<CompressionApplicationService>();
}

// Контроллер для API
[ApiController]
[Route("api/[controller]")]
public class CompressionController : ControllerBase
{
    private readonly CompressionApplicationService _compressionService;

    [HttpPost("compress")]
    public async Task<IActionResult> CompressFile([FromBody] CompressRequest request)
    {
        var result = await _compressionService.CompressFileAsync(request.FilePath);
        
        if (result.IsSuccess)
        {
            return Ok(new { Message = "File compressed successfully", JobId = result.Job.Id });
        }
        
        return BadRequest(new { Error = result.ErrorMessage });
    }
}
```

### 2. Интеграция с Windows Service

```csharp
// Windows Service для автоматического сжатия
public class CompressionWindowsService : BackgroundService
{
    private readonly CompressionApplicationService _compressionService;
    private readonly ILogger<CompressionWindowsService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CompressOldFiles();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Каждый час
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in compression service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task CompressOldFiles()
    {
        var oldFiles = Directory.GetFiles("C:\\Temp", "*.*")
            .Where(file => File.GetCreationTime(file) < DateTime.Now.AddDays(-1))
            .ToArray();

        foreach (var file in oldFiles)
        {
            await _compressionService.CompressFileAsync(file);
        }
    }
}
```

### 3. Интеграция с Message Queue

```csharp
// Обработка сообщений из очереди
public class CompressionMessageHandler
{
    private readonly CompressionApplicationService _compressionService;

    public async Task HandleCompressionMessage(CompressionMessage message)
    {
        try
        {
            var settings = new CompressionSettings(
                bufferSize: message.BufferSize,
                maxDegreeOfParallelism: message.MaxParallelism
            );

            var result = await _compressionService.CompressFileAsync(message.FilePath, settings);
            
            // Отправка результата обратно в очередь
            await SendResultMessage(new CompressionResultMessage
            {
                JobId = message.JobId,
                IsSuccess = result.IsSuccess,
                ErrorMessage = result.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            await SendErrorMessage(message.JobId, ex.Message);
        }
    }
}
```

## 🧪 Тестирование

### 1. Unit тесты

```csharp
[TestFixture]
public class CompressionServiceTests
{
    private CompressionService _service;
    private Mock<ICompressionAlgorithm> _mockAlgorithm;
    private Mock<IFileService> _mockFileService;
    private Mock<IProgressReporter> _mockProgressReporter;

    [SetUp]
    public void Setup()
    {
        _mockAlgorithm = new Mock<ICompressionAlgorithm>();
        _mockFileService = new Mock<IFileService>();
        _mockProgressReporter = new Mock<IProgressReporter>();
        
        _service = new CompressionService(
            _mockAlgorithm.Object,
            _mockFileService.Object,
            _mockProgressReporter.Object
        );
    }

    [Test]
    public async Task CompressAsync_ValidJob_ReturnsCompletedJob()
    {
        // Arrange
        var job = CreateTestJob();
        _mockFileService.Setup(x => x.ExistsAsync(It.IsAny<FileInfo>()))
                       .ReturnsAsync(true);

        // Act
        var result = await _service.CompressAsync(job);

        // Assert
        Assert.AreEqual(CompressionStatus.Completed, result.Status);
        _mockProgressReporter.Verify(x => x.Report(It.IsAny<double>()), Times.AtLeastOnce);
    }
}
```

### 2. Integration тесты

```csharp
[TestFixture]
public class CompressionIntegrationTests
{
    private ServiceProvider _serviceProvider;

    [SetUp]
    public void Setup()
    {
        _serviceProvider = ServiceConfiguration.ConfigureServices();
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public async Task EndToEndCompression_ValidFile_ReturnsSuccess()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<CompressionApplicationService>();
        var testFile = CreateTestFile();

        // Act
        var result = await service.CompressFileAsync(testFile);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(File.Exists(testFile + ".gz"));
    }
}
```

### 3. Performance тесты

```csharp
[TestFixture]
public class CompressionPerformanceTests
{
    [Test]
    public void CompressLargeFile_PerformanceWithinLimits()
    {
        // Arrange
        var largeFile = CreateLargeTestFile(100 * 1024 * 1024); // 100MB
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = compressionService.CompressFileAsync(largeFile).Result;

        // Assert
        stopwatch.Stop();
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 30000); // Менее 30 секунд
    }
}
```

## 📊 Мониторинг и метрики

### 1. Сбор метрик

```csharp
public class CompressionMetrics
{
    private static readonly Counter ProcessedBytesCounter = 
        Meter.CreateCounter("compression_processed_bytes_total");
    
    private static readonly Histogram CompressionDuration = 
        Meter.CreateHistogram("compression_duration_seconds");
    
    private static readonly Counter CompressionErrors = 
        Meter.CreateCounter("compression_errors_total");

    public static void RecordCompression(long bytesProcessed, TimeSpan duration, bool isSuccess)
    {
        ProcessedBytesCounter.Add(bytesProcessed);
        CompressionDuration.Record(duration.TotalSeconds);
        
        if (!isSuccess)
        {
            CompressionErrors.Add(1);
        }
    }
}
```

### 2. Health Checks

```csharp
public class CompressionHealthCheck : IHealthCheck
{
    private readonly IFileService _fileService;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Проверка доступности файловой системы
            var testFile = new FileInfo("health_check_test.tmp");
            await _fileService.CreateAsync(testFile);
            await _fileService.DeleteAsync(testFile);
            
            return HealthCheckResult.Healthy("Compression service is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Compression service is unhealthy", ex);
        }
    }
}
```

## 🔧 Конфигурация для разных сценариев

### 1. Высокая производительность

```csharp
var highPerformanceSettings = new CompressionSettings(
    bufferSize: 131072,           // 128KB
    maxBufferSize: 33554432,      // 32MB
    boundedCapacity: 500,         // Высокая емкость
    maxDegreeOfParallelism: Environment.ProcessorCount * 2
);
```

### 2. Низкое использование памяти

```csharp
var lowMemorySettings = new CompressionSettings(
    bufferSize: 4096,             // 4KB
    maxBufferSize: 1048576,       // 1MB
    boundedCapacity: 25,          // Низкая емкость
    maxDegreeOfParallelism: 1     // Последовательная обработка
);
```

### 3. Сбалансированная конфигурация

```csharp
var balancedSettings = new CompressionSettings(
    bufferSize: 32768,            // 32KB
    maxBufferSize: 8388608,       // 8MB
    boundedCapacity: 100,         // Средняя емкость
    maxDegreeOfParallelism: Environment.ProcessorCount
);
```

Эти примеры демонстрируют гибкость и расширяемость архитектуры Buser.AsyncCompression, позволяя адаптировать приложение под различные сценарии использования.
