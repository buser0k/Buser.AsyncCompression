# CI/CD Pipeline Documentation

## Обзор

Проект Buser.AsyncCompression использует GitHub Actions для автоматической сборки, тестирования и публикации. Pipeline настроен с использованием Cake для кроссплатформенной автоматизации сборки.

## Триггеры

Pipeline запускается при:
- Push в ветки `main` или `develop`
- Pull Request в ветки `main` или `develop`

## Этапы Pipeline

### 1. Build and Test (Ubuntu)
- **OS**: Ubuntu Latest
- **Действия**:
  - Checkout кода
  - Установка .NET 8.0
  - Кэширование NuGet пакетов
  - Установка Cake
  - Восстановление зависимостей
  - Сборка решения
  - Запуск тестов
  - Генерация отчетов о покрытии кода
  - Загрузка результатов тестов

### 2. Build Windows
- **OS**: Windows Latest
- **Действия**: Аналогично Ubuntu, но на Windows

### 3. Build macOS
- **OS**: macOS Latest
- **Действия**: Аналогично Ubuntu, но на macOS

### 4. Publish (только для main)
- **Условие**: Push в main ветку
- **Действия**:
  - Создание NuGet пакета
  - Публикация в NuGet (если настроен API ключ)
  - Создание GitHub Release

## Cake Build Script

### Доступные задачи (Targets)

- **Default**: Запускает полный цикл тестирования
- **Clean**: Очищает артефакты сборки
- **Restore**: Восстанавливает NuGet пакеты
- **Build**: Собирает решение
- **Test**: Запускает тесты с покрытием кода
- **TestResults**: Генерация отчетов о тестах
- **Package**: Создает пакет приложения
- **Publish**: Публикует в NuGet

### Локальный запуск

#### Windows (PowerShell)
```powershell
# Установка Cake
dotnet tool install --global Cake.Tool --version 3.0.0

# Запуск сборки
.\build.ps1

# Запуск с параметрами
.\build.ps1 -Target Test -Configuration Debug
```

#### Linux/macOS (Bash)
```bash
# Установка Cake
dotnet tool install --global Cake.Tool --version 3.0.0

# Запуск сборки
./build.sh

# Запуск с параметрами
./build.sh --target Test --configuration Debug
```

#### Прямой запуск через dotnet
```bash
# Установка Cake
dotnet tool install --global Cake.Tool --version 3.0.0

# Запуск
dotnet cake build.cake --target=Test --configuration=Debug
```

## Конфигурация

### Покрытие кода
- **Минимальный порог**: 80%
- **Формат отчетов**: HTML, Cobertura
- **Исключения**: Тестовые проекты, автогенерированный код

### Кэширование
- NuGet пакеты кэшируются для ускорения сборки
- Ключ кэша основан на хэше .csproj файлов

## Секреты GitHub

Для публикации в NuGet необходимо настроить секрет:
- `NUGET_API_KEY`: API ключ для NuGet.org

## Артефакты

Pipeline создает следующие артефакты:
- **test-results**: Результаты тестов в формате TRX
- **coverage**: Отчеты о покрытии кода
- **artifacts**: Собранные пакеты приложения

## Мониторинг

- Статус сборки отображается в GitHub Actions
- Результаты тестов доступны в разделе Actions
- Отчеты о покрытии кода загружаются как артефакты

## Troubleshooting

### Частые проблемы

1. **Ошибки сборки**: Проверьте логи в GitHub Actions
2. **Проблемы с тестами**: Убедитесь, что все тесты проходят локально
3. **Ошибки публикации**: Проверьте настройку секретов

### Локальная отладка

```bash
# Запуск с подробным выводом
dotnet cake build.cake --verbosity=Diagnostic

# Сухой прогон (показывает что будет выполнено)
dotnet cake build.cake --dryrun
```
