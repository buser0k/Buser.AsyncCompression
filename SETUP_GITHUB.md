# Настройка GitHub репозитория

## Создание репозитория

1. Перейдите на [GitHub](https://github.com/buser0k?tab=repositories)
2. Нажмите кнопку "New" для создания нового репозитория
3. Заполните данные:
   - **Repository name**: `Booser.AsyncCompression`
   - **Description**: `High-performance async file compression application with Clean Architecture and comprehensive test suite`
   - **Visibility**: Public (или Private по желанию)
   - **Initialize**: НЕ отмечайте "Add a README file" (у нас уже есть)

## Настройка локального репозитория

После создания репозитория на GitHub выполните следующие команды:

```bash
# Добавьте remote origin (замените YOUR_USERNAME на ваш GitHub username)
git remote add origin https://github.com/buser0k/Booser.AsyncCompression.git

# Переименуйте ветку в main (если нужно)
git branch -M main

# Отправьте код в репозиторий
git push -u origin main
```

## Настройка секретов (опционально)

Если планируете публикацию в NuGet:

1. Перейдите в Settings → Secrets and variables → Actions
2. Добавьте новый секрет:
   - **Name**: `NUGET_API_KEY`
   - **Value**: Ваш API ключ от NuGet.org

## Проверка CI/CD

После первого push:

1. Перейдите в раздел "Actions" вашего репозитория
2. Убедитесь, что pipeline запустился
3. Проверьте, что все этапы прошли успешно

## Структура репозитория

После настройки ваш репозиторий будет содержать:

```
Booser.AsyncCompression/
├── .github/
│   └── workflows/
│       └── ci.yml              # GitHub Actions pipeline
├── Booser.AsyncCompression/    # Основной проект
├── Booser.AsyncCompression.Tests/ # Тесты
├── build.cake                  # Cake build script
├── build.ps1                   # PowerShell скрипт для Windows
├── build.sh                    # Bash скрипт для Unix/Linux
├── coverlet.runsettings        # Настройки покрытия кода
├── CI-CD.md                    # Документация по CI/CD
└── README.md                   # Основная документация
```

## Команды для работы с репозиторием

### Основные команды Git
```bash
# Проверка статуса
git status

# Добавление изменений
git add .

# Коммит
git commit -m "Описание изменений"

# Отправка в репозиторий
git push

# Получение изменений
git pull
```

### Работа с ветками
```bash
# Создание новой ветки
git checkout -b feature/new-feature

# Переключение на ветку
git checkout main

# Слияние ветки
git merge feature/new-feature
```

### Локальная сборка и тестирование
```bash
# Сборка проекта
dotnet build

# Запуск тестов
dotnet test

# Запуск через Cake
dotnet cake build.cake --target=Test
```

## Мониторинг

- **Actions**: Статус CI/CD pipeline
- **Issues**: Отслеживание багов и задач
- **Pull Requests**: Код-ревью и слияние изменений
- **Releases**: Версионирование и публикация

## Дополнительные настройки

### Защита ветки main
1. Settings → Branches
2. Add rule для main
3. Включите "Require pull request reviews"
4. Включите "Require status checks to pass before merging"

### Настройка уведомлений
1. Settings → Notifications
2. Настройте уведомления о CI/CD статусе
3. Настройте уведомления о Issues и PR
