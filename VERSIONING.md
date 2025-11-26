# Версионирование проекта

Проект использует автоматическое версионирование при merge в ветку `main`.

## Семантическое версионирование (SemVer)

Проект следует [Semantic Versioning](https://semver.org/):
- **MAJOR** (X.0.0) - несовместимые изменения API
- **MINOR** (0.X.0) - новая функциональность с обратной совместимостью
- **PATCH** (0.0.X) - исправления ошибок с обратной совместимостью

## Автоматическое определение типа версии

При merge в `main` версия автоматически увеличивается на основе сообщений коммитов:

### Major (X.0.0)
Увеличивается при наличии в сообщении коммита:
- `breaking`
- `major`
- `BREAKING`

**Примеры:**
```
breaking: изменен API интерфейса
major: несовместимые изменения
BREAKING CHANGE: удален метод
```

### Minor (0.X.0)
Увеличивается при наличии в сообщении коммита:
- `feat:` или `feature:`

**Примеры:**
```
feat: добавлена поддержка Brotli
feature: новый алгоритм сжатия
```

### Patch (0.0.X)
Используется по умолчанию для:
- `fix:` - исправления ошибок
- `refactor:` - рефакторинг
- `docs:` - документация
- `test:` - тесты
- `chore:` - технические изменения

**Примеры:**
```
fix: исправлена ошибка в CompressionService
refactor: улучшена архитектура
docs: обновлена документация
```

## Текущая версия

Текущая целевая версия пакета указана в файле `Buser.AsyncCompression/Buser.AsyncCompression.csproj`:
```xml
<Version>3.0.1</Version>
<AssemblyVersion>3.0.1</AssemblyVersion>
<FileVersion>3.0.1</FileVersion>
```

> Фактическая опубликованная на NuGet версия может быть выше, если CI/CD уже выполнял
> автоматический bump версии при предыдущих merge в `main`.

## Процесс версионирования

1. **При merge в main:**
   - CI/CD автоматически определяет тип версии из сообщений коммитов
   - Версия увеличивается в `.csproj` файле
   - Создается NuGet пакет с новой версией
   - Пакет публикуется в NuGet.org (если настроен API ключ)
   - Создается GitHub Release с тегом версии
   - Изменения версии коммитятся обратно в репозиторий

2. **Ручное управление версией:**
   ```powershell
   # Windows
   .\scripts\increment-version.ps1 -VersionBump patch
   .\scripts\increment-version.ps1 -VersionBump minor
   .\scripts\increment-version.ps1 -VersionBump major
   ```
   
   ```bash
   # Linux/macOS
   ./scripts/increment-version.sh patch
   ./scripts/increment-version.sh minor
   ./scripts/increment-version.sh major
   ```

## Рекомендации по коммитам

Для правильного автоматического версионирования используйте [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` - новая функциональность (minor)
- `fix:` - исправление ошибки (patch)
- `refactor:` - рефакторинг (patch)
- `docs:` - документация (patch)
- `test:` - тесты (patch)
- `chore:` - технические изменения (patch)
- `breaking:` или `BREAKING CHANGE:` - несовместимые изменения (major)

## Проверка версии

Текущую версию можно проверить:
```bash
# Через Cake
dotnet cake build.cake --target=Clean

# Через grep
grep -oP '<Version>\K[^<]+' Buser.AsyncCompression/Buser.AsyncCompression.csproj
```

## Troubleshooting

### Версия не увеличивается
- Убедитесь, что коммит в ветку `main`
- Проверьте формат сообщения коммита
- Проверьте логи CI/CD в GitHub Actions

### Ошибка при публикации в NuGet
- Версия уже существует в NuGet - версия автоматически увеличивается
- Отсутствует NUGET_API_KEY - публикация пропускается (ожидаемо для forks)

