# Быстрый старт Buser.AsyncCompression

## 🚀 Запуск за 5 минут

### 1. Предварительные требования
- .NET 8.0 SDK или Runtime
- Windows 10/11, Linux или macOS

### 2. Клонирование и сборка
```bash
git clone <repository-url>
cd Buser.AsyncCompression
dotnet build
```

### 3. Создание тестового файла
```bash
echo "Hello, World! This is a test file for compression." > test.txt
```

### 4. Запуск сжатия
```bash
dotnet run
```

### 5. Проверка результата
```bash
ls *.gz
# Должен появиться файл test.txt.gz
```

## 🎮 Управление процессом

Во время сжатия нажмите:
- **P** — приостановить
- **R** — возобновить  
- **X** — отменить

## 📁 Структура проекта

```
Buser.AsyncCompression/
├── 📁 Application/          # Слой приложения
├── 📁 Domain/              # Доменный слой
├── 📁 Infrastructure/      # Инфраструктурный слой
├── 📄 Program.cs           # Точка входа
└── 📄 *.md                 # Документация
```

## 🔧 Основные настройки

```csharp
// В Program.cs можно изменить настройки
var settings = new CompressionSettings(
    bufferSize: 8192,        // Размер буфера
    maxDegreeOfParallelism: Environment.ProcessorCount // Параллелизм
);
```

## 📚 Дополнительная документация

- [README.md](README.md) — Полное описание проекта
- [ARCHITECTURE.md](ARCHITECTURE.md) — Детальная архитектура
- [EXAMPLES.md](EXAMPLES.md) — Примеры использования
- [TECHNICAL_SPECS.md](TECHNICAL_SPECS.md) — Технические спецификации

## ❓ Проблемы?

1. **Ошибка "File not found"** — Убедитесь, что файл существует
2. **Медленная работа** — Увеличьте `maxDegreeOfParallelism`
3. **Нехватка памяти** — Уменьшите `bufferSize`

## 🎯 Что дальше?

- Изучите [EXAMPLES.md](EXAMPLES.md) для расширенных сценариев
- Добавьте собственные алгоритмы сжатия
- Интегрируйте с вашими приложениями
- Внедрите мониторинг и логирование
