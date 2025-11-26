# Buser.AsyncCompression

## 📋 Описание проекта

**Buser.AsyncCompression** — это консольное приложение для асинхронного сжатия файлов, построенное с использованием принципов **Domain-Driven Design (DDD)** и **SOLID**. Проект демонстрирует современные подходы к архитектуре .NET приложений с применением паттернов проектирования и dependency injection - слава AI! :).

### 🎯 Основные возможности

- **Асинхронное сжатие файлов** с использованием TPL Dataflow
- **Многопоточная обработка** данных для максимальной производительности
- **Интерактивное управление** процессом сжатия (пауза, возобновление, отмена)
- **Визуальный прогресс-бар** с анимацией
- **Модульная архитектура** с возможностью легкого добавления новых алгоритмов сжатия
- **Рекурсивное сжатие папок** с сохранением структуры каталогов
- **Dependency Injection** для управления зависимостями
- **Современный .NET 8** с nullable reference types

### 🚀 Быстрый старт

```bash
# Клонирование репозитория
git clone <repository-url>
cd Buser.AsyncCompression

# Сборка проекта
dotnet build

# Запуск с файлом для сжатия
dotnet run <путь_к_файлу>

# Запуск с директорией (будут сжаты все файлы рекурсивно)
dotnet run <путь_к_папке>

# Пример
dotnet run test.txt
```

### 🎮 Управление процессом

Во время сжатия доступны следующие команды:
- **P** — приостановить сжатие
- **R** — возобновить сжатие  
- **X** — отменить сжатие

## 🏗️ Архитектура проекта

Проект построен по принципам **Clean Architecture** с четким разделением на слои:

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│                     (Program.cs)                            │
└─────────────────────────────────────────────────────────────┘
                                │
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                         │
│  ┌─────────────────┐  ┌─────────────────────────────────┐   │
│  │   Services      │  │         Factories               │   │
│  │                 │  │                                 │   │
│  │ • Compression   │  │ • CompressionJobFactory         │   │
│  │   Service       │  │ • CompressionAlgorithmFactory   │   │
│  │ • Compression   │  │                                 │   │
│  │   Application   │  │                                 │   │
│  │   Service       │  │                                 │   │
│  └─────────────────┘  └─────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                                │
┌───────────────────────────────────────────────────────────────┐
│                     Domain Layer                              │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────────┐  │
│  │  Entities    │  │ Value        │  │    Interfaces       │  │
│  │              │  │ Objects      │  │                     │  │
│  │ • Compression│  │ • FileInfo   │  │ • ICompression      │  │
│  │   Job        │  │ • Compression│  │   Algorithm         │  │
│  │              │  │   Settings   │  │ • IFileService      │  │
│  │              │  │              │  │ • ICompression      │  │
│  │              │  │              │  │   Service           │  │
│  │              │  │              │  │ • IProgressReporter │  │
│  └──────────────┘  └──────────────┘  └─────────────────────┘  │
└───────────────────────────────────────────────────────────────┘
                                │
┌───────────────────────────────────────────────────────────────┐
│                 Infrastructure Layer                          │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────────┐  │
│  │  Services    │  │ Algorithms   │  │   Dependency        │  │
│  │              │  │              │  │   Injection         │  │
│  │ • FileService│  │ • GZip       │  │ • ServiceConfig     │  │
│  │              │  │   Compression│  │                     │  │
│  │              │  │   Algorithm  │  │                     │  │
│  └──────────────┘  └──────────────┘  └─────────────────────┘  │
└───────────────────────────────────────────────────────────────┘
```

## 📁 Структура проекта

```
Buser.AsyncCompression/
├── 📁 Application/                          # Слой приложения
│   ├── 📁 Factories/                        # Фабрики для создания объектов
│   │   ├── CompressionAlgorithmFactory.cs   # Фабрика алгоритмов сжатия
│   │   └── CompressionJobFactory.cs         # Фабрика задач сжатия
│   └── 📁 Services/                         # Сервисы приложения
│       ├── CompressionApplicationService.cs # Основной сервис приложения
│       └── CompressionService.cs            # Доменный сервис сжатия
│
├── 📁 Domain/                               # Доменный слой
│   ├── 📁 Entities/                         # Сущности
│   │   └── CompressionJob.cs                # Агрегат задачи сжатия
│   ├── 📁 Interfaces/                       # Доменные интерфейсы
│   │   ├── ICompressionAlgorithm.cs         # Интерфейс алгоритма сжатия
│   │   ├── ICompressionService.cs           # Интерфейс сервиса сжатия
│   │   ├── IFileService.cs                  # Интерфейс сервиса файлов
│   │   └── IProgressReporter.cs             # Интерфейс отчета о прогрессе
│   └── 📁 ValueObjects/                     # Объекты-значения
│       ├── CompressionSettings.cs           # Настройки сжатия
│       └── FileInfo.cs                      # Информация о файле
│
├── 📁 Infrastructure/                       # Инфраструктурный слой
│   ├── 📁 Algorithms/                       # Реализации алгоритмов
│   │   └── GZipCompressionAlgorithm.cs      # GZip алгоритм сжатия
│   ├── 📁 DI/                              # Dependency Injection
│   │   └── ServiceConfiguration.cs          # Конфигурация сервисов
│   └── 📁 Services/                        # Реализации сервисов
│       └── FileService.cs                   # Сервис работы с файлами
│
├── 📄 Program.cs                            # Точка входа приложения
├── 📄 ProgressBar.cs                        # Компонент прогресс-бара
├── 📄 TaskCompression.cs                    # Устаревший класс (legacy)
├── 📄 GZipCompressionAlgorithm.cs           # Устаревший класс (legacy)
├── 📄 ICompressionAlgorithm.cs              # Устаревший интерфейс (legacy)
└── 📄 Buser.AsyncCompression.csproj         # Файл проекта
```

## 🔧 Технические детали

### Используемые технологии

- **.NET 8** — современная платформа разработки
- **Microsoft.Extensions.DependencyInjection** — встроенный DI контейнер
- **System.Threading.Tasks.Dataflow** — для асинхронной обработки данных
- **TPL (Task Parallel Library)** — для параллельного выполнения задач

### Применяемые паттерны

#### 1. **Domain-Driven Design (DDD)**
- **Entities**: `CompressionJob` — агрегат с бизнес-логикой
- **Value Objects**: `FileInfo`, `CompressionSettings` — неизменяемые объекты
- **Domain Services**: `CompressionService` — бизнес-логика сжатия
- **Application Services**: `CompressionApplicationService` — координация операций

#### 2. **SOLID принципы**
- **SRP**: Каждый класс имеет одну ответственность
- **OCP**: Легко расширяется новыми алгоритмами
- **LSP**: Все реализации интерфейсов взаимозаменяемы
- **ISP**: Интерфейсы разделены по функциональности
- **DIP**: Зависимости инвертированы через DI

#### 3. **Дополнительные паттерны**
- **Factory Pattern**: Создание объектов через фабрики
- **Repository Pattern**: Абстракция доступа к данным
- **Strategy Pattern**: Различные алгоритмы сжатия
- **Observer Pattern**: Отслеживание прогресса

### Архитектурные решения

#### 1. **Слоистая архитектура**
```
Presentation → Application → Domain ← Infrastructure
```

#### 2. **Dependency Injection**
```csharp
// Регистрация сервисов
services.AddSingleton<IFileService, FileService>();
services.AddSingleton<ICompressionAlgorithm, GZipCompressionAlgorithm>();
services.AddTransient<ICompressionService, CompressionService>();
```

#### 3. **Асинхронная обработка**
```csharp
// Пайплайн обработки данных
BufferBlock → TransformBlock → ActionBlock
```

## 🚀 Расширение функциональности

### Добавление нового алгоритма сжатия

1. **Создать реализацию интерфейса**:
```csharp
public class LZ4CompressionAlgorithm : ICompressionAlgorithm
{
    public string Name => "LZ4";
    public string FileExtension => ".lz4";
    
    public byte[] Compress(byte[] bytes)
    {
        // Реализация LZ4 сжатия
    }
}
```

2. **Зарегистрировать в DI контейнере**:
```csharp
services.AddSingleton<ICompressionAlgorithm, LZ4CompressionAlgorithm>();
```

3. **Обновить фабрику алгоритмов**:
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

### Добавление новых настроек

1. **Расширить Value Object**:
```csharp
public class CompressionSettings
{
    public int BufferSize { get; }
    public CompressionLevel Level { get; } // Новое свойство
    // ...
}
```

2. **Обновить конструкторы и методы**

## 📊 Производительность

### Оптимизации

- **Параллельная обработка**: Использование `MaxDegreeOfParallelism = Environment.ProcessorCount`
- **Буферизация**: Настраиваемый размер буфера (по умолчанию 8KB)
- **Ограниченная емкость**: Предотвращение переполнения памяти
- **Асинхронные операции**: Неблокирующие I/O операции

### Метрики

- **Время сжатия**: Зависит от размера файла и алгоритма
- **Использование памяти**: Контролируемое через настройки буфера
- **CPU утилизация**: Оптимизирована для многоядерных систем

## 🧪 Тестирование

### Рекомендуемые тесты

1. **Unit тесты** для каждого компонента
2. **Integration тесты** для проверки взаимодействия слоев
3. **Performance тесты** для измерения производительности
4. **End-to-end тесты** для полного цикла сжатия

### Пример теста

```csharp
[Test]
public async Task CompressFileAsync_ValidFile_ReturnsSuccess()
{
    // Arrange
    var service = new CompressionApplicationService(/* dependencies */);
    
    // Act
    var result = await service.CompressFileAsync("test.txt");
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
}
```

## 📝 Лицензия

Проект создан в образовательных целях для демонстрации современных подходов к архитектуре .NET приложений.

## 🤝 Вклад в проект

1. Fork репозитория
2. Создать feature branch
3. Внести изменения
4. Добавить тесты
5. Создать Pull Request

## 📞 Контакты

Для вопросов и предложений создавайте Issues в репозитории проекта.
