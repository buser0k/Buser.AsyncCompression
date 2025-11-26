# Архитектура Buser.AsyncCompression

## 🏛️ Обзор архитектуры

Проект построен на основе принципов **Clean Architecture** и **Domain-Driven Design (DDD)**, обеспечивая высокую модульность, тестируемость и расширяемость.

## 📐 Диаграмма архитектуры

```
┌─────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                Program.cs                               │   │
│  │  • Точка входа приложения                              │   │
│  │  • Конфигурация DI контейнера                          │   │
│  │  • Обработка пользовательского ввода                   │   │
│  │  • Управление жизненным циклом приложения              │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      APPLICATION LAYER                          │
│  ┌─────────────────────────┐  ┌─────────────────────────────┐   │
│  │      SERVICES           │  │        FACTORIES            │   │
│  │                         │  │                             │   │
│  │ • CompressionService    │  │ • CompressionJobFactory     │   │
│  │   - Бизнес-логика       │  │   - Создание задач сжатия   │   │
│  │   - Управление потоком  │  │                             │   │
│  │                         │  │ • CompressionAlgorithm      │   │
│  │ • CompressionApplication│  │   Factory                   │   │
│  │   Service               │  │   - Создание алгоритмов     │   │
│  │   - Координация         │  │                             │   │
│  │   - Use Cases           │  │                             │   │
│  └─────────────────────────┘  └─────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                        DOMAIN LAYER                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐   │
│  │  ENTITIES   │  │ VALUE       │  │      INTERFACES         │   │
│  │             │  │ OBJECTS     │  │                         │   │
│  │ • Compression│  │ • FileInfo  │  │ • ICompressionAlgorithm │   │
│  │   Job        │  │   - Путь    │  │   - Compress()          │   │
│  │   - Id       │  │   - Размер  │  │   - Name                │   │
│  │   - Status   │  │   - Существ.│  │   - FileExtension       │   │
│  │   - Progress │  │             │  │                         │   │
│  │   - Methods  │  │ • Compression│  │ • IFileService          │   │
│  │             │  │   Settings   │  │   - OpenReadAsync()     │   │
│  │             │  │   - Buffer   │  │   - CreateAsync()       │   │
│  │             │  │   - Parallel │  │   - ExistsAsync()       │   │
│  │             │  │   - Capacity │  │   - DeleteAsync()       │   │
│  │             │  │             │  │                         │   │
│  │             │  │             │  │ • ICompressionService    │   │
│  │             │  │             │  │   - CompressAsync()      │   │
│  │             │  │             │  │   - Pause()              │   │
│  │             │  │             │  │   - Resume()             │   │
│  │             │  │             │  │   - Cancel()             │   │
│  │             │  │             │  │                         │   │
│  │             │  │             │  │ • IProgressReporter      │   │
│  │             │  │             │  │   - Report()             │   │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    INFRASTRUCTURE LAYER                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐   │
│  │  SERVICES   │  │ ALGORITHMS  │  │   DEPENDENCY INJECTION  │   │
│  │             │  │             │  │                         │   │
│  │ • FileService│  │ • GZip      │  │ • ServiceConfiguration  │   │
│  │   - Реализация│  │   Compression│  │   - ConfigureServices() │   │
│  │     IFileService│  │   Algorithm │  │   - Регистрация DI     │   │
│  │   - System.IO│  │   - Compress()│  │   - Lifecycle управление│   │
│  │     операции │  │   - Name     │  │                         │   │
│  │             │  │   - Extension │  │                         │   │
│  │             │  │             │  │                         │   │
│  │             │  │             │  │                         │   │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

## 🔄 Поток данных

### 1. Инициализация
```
Program.cs → ServiceConfiguration → DI Container → Services Registration
```

### 2. Создание задачи сжатия
```
User Input → CompressionJobFactory → CompressionJob Entity
```

### 3. Процесс сжатия
```
Input File → FileService → BufferBlock → TransformBlock → ActionBlock → Output File
```

### 4. Отслеживание прогресса
```
CompressionService → IProgressReporter → ProgressBar → Console Output
```

## 🎯 Ключевые компоненты

### Domain Layer

#### CompressionJob (Entity)
```csharp
public class CompressionJob
{
    public Guid Id { get; }
    public FileInfo InputFile { get; }
    public FileInfo OutputFile { get; }
    public CompressionSettings Settings { get; }
    public CompressionStatus Status { get; private set; }
    
    // Бизнес-методы
    public void Start();
    public void Pause();
    public void Resume();
    public void Complete();
    public void Cancel();
    public void UpdateProgress(long processedBytes);
}
```

#### FileInfo (Value Object)
```csharp
public class FileInfo
{
    public string FullPath { get; }
    public string Name { get; }
    public long Size { get; }
    public bool Exists { get; }
    
    // Неизменяемый объект с валидацией
}
```

#### CompressionSettings (Value Object)
```csharp
public class CompressionSettings
{
    public int BufferSize { get; }
    public int MaxBufferSize { get; }
    public int BoundedCapacity { get; }
    public int MaxDegreeOfParallelism { get; }
    
    // Инкапсуляция настроек с валидацией
}
```

### Application Layer

#### CompressionApplicationService
```csharp
public class CompressionApplicationService
{
    // Координация операций
    public async Task<CompressionResult> CompressFileAsync(string inputFilePath, CompressionSettings? settings = null);
    public void PauseCompression(CompressionJob job);
    public void ResumeCompression(CompressionJob job);
    public void CancelCompression(CompressionJob job);
}
```

#### CompressionService
```csharp
public class CompressionService : ICompressionService
{
    // Доменная логика сжатия
    public async Task<CompressionJob> CompressAsync(CompressionJob job);
    public void Pause(CompressionJob job);
    public void Resume(CompressionJob job);
    public void Cancel(CompressionJob job);
}
```

### Infrastructure Layer

#### TPL Dataflow Pipeline
```csharp
// Пайплайн обработки данных
var buffer = new BufferBlock<byte[]>();
var compressor = new TransformBlock<byte[], byte[]>(bytes => algorithm.Compress(bytes));
var writer = new ActionBlock<byte[]>(bytes => outputStream.Write(bytes));

// Связывание блоков
buffer.LinkTo(compressor);
compressor.LinkTo(writer);
```

## 🔧 Dependency Injection

### Конфигурация сервисов
```csharp
public static ServiceProvider ConfigureServices(IProgressReporter? progressReporter = null)
{
    var services = new ServiceCollection();
    
    // Регистрация сервисов
    services.AddSingleton<IFileService, FileService>();
    services.AddSingleton<ICompressionAlgorithm, GZipCompressionAlgorithm>();
    services.AddTransient<ICompressionService, CompressionService>();
    
    // Регистрация фабрик
    services.AddSingleton<CompressionJobFactory>();
    services.AddSingleton<CompressionAlgorithmFactory>();
    
    return services.BuildServiceProvider();
}
```

### Жизненный цикл объектов
- **Singleton**: `IFileService`, `ICompressionAlgorithm`, фабрики
- **Transient**: `ICompressionService` (для каждого запроса)
- **Scoped**: `IProgressReporter` (для сессии сжатия)

## 🚀 Расширяемость

### Добавление нового алгоритма сжатия

1. **Реализация интерфейса**:
```csharp
public class LZ4CompressionAlgorithm : ICompressionAlgorithm
{
    public string Name => "LZ4";
    public string FileExtension => ".lz4";
    
    public byte[] Compress(byte[] bytes)
    {
        // Реализация LZ4
    }
}
```

2. **Регистрация в DI**:
```csharp
services.AddSingleton<ICompressionAlgorithm, LZ4CompressionAlgorithm>();
```

3. **Обновление фабрики**:
```csharp
public ICompressionAlgorithm CreateAlgorithm(string algorithmName)
{
    return algorithmName?.ToLower() switch
    {
        "gzip" => CreateGZipAlgorithm(),
        "lz4" => CreateLZ4Algorithm(),
        _ => CreateGZipAlgorithm()
    };
}
```

### Добавление новых источников данных

1. **Создание интерфейса**:
```csharp
public interface IDataSource
{
    Task<Stream> OpenReadAsync(string source);
    Task<Stream> CreateAsync(string destination);
}
```

2. **Реализация**:
```csharp
public class NetworkDataSource : IDataSource
{
    // Реализация для сетевых источников
}
```

3. **Интеграция в сервисы**

## 🧪 Тестируемость

### Unit тесты
```csharp
[Test]
public void CompressionJob_Start_ChangesStatusToRunning()
{
    // Arrange
    var job = new CompressionJob(inputFile, outputFile, settings);
    
    // Act
    job.Start();
    
    // Assert
    Assert.AreEqual(CompressionStatus.Running, job.Status);
}
```

### Integration тесты
```csharp
[Test]
public async Task CompressionService_CompressAsync_ReturnsCompletedJob()
{
    // Arrange
    var service = new CompressionService(algorithm, fileService, progressReporter);
    
    // Act
    var result = await service.CompressAsync(job);
    
    // Assert
    Assert.AreEqual(CompressionStatus.Completed, result.Status);
}
```

## 📊 Производительность

### Оптимизации
- **Параллельная обработка**: `MaxDegreeOfParallelism = Environment.ProcessorCount`
- **Буферизация**: Настраиваемый размер буфера
- **Ограниченная емкость**: Предотвращение переполнения памяти
- **Асинхронные операции**: Неблокирующие I/O

### Метрики
- **Время сжатия**: Логарифмическая зависимость от размера файла
- **Использование памяти**: Линейная зависимость от размера буфера
- **CPU утилизация**: Оптимизирована для многоядерных систем

## 🔒 Безопасность

### Валидация входных данных
```csharp
public FileInfo(string path)
{
    if (string.IsNullOrWhiteSpace(path))
        throw new ArgumentException("File path cannot be null or empty", nameof(path));
    
    FullPath = System.IO.Path.GetFullPath(path);
    // Дополнительная валидация
}
```

### Обработка ошибок
```csharp
try
{
    var result = await compressionService.CompressAsync(job);
    return CompressionResult.Success(result);
}
catch (Exception ex)
{
    return CompressionResult.Failed($"Compression failed: {ex.Message}");
}
```

## 📈 Мониторинг и логирование

### Рекомендуемые улучшения
1. **Структурированное логирование** с использованием Serilog
2. **Метрики производительности** с помощью Application Insights
3. **Health checks** для мониторинга состояния сервисов
4. **Distributed tracing** для отслеживания запросов

## 🔮 Будущие улучшения

### Планируемые функции
1. **Поддержка множественных алгоритмов** сжатия
2. **REST API** для удаленного управления
3. **Web UI** для веб-интерфейса
4. **Планировщик задач** для автоматического сжатия
5. **Кластеризация** для горизонтального масштабирования

### Технические улучшения
1. **Микросервисная архитектура**
2. **Event Sourcing** для аудита операций
3. **CQRS** для разделения команд и запросов
4. **Message Queues** для асинхронной обработки
5. **Containerization** с Docker
