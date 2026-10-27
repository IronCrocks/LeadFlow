# LeadFlow

LeadFlow — MVP для приёма заявок с лендинга и их обработки менеджером.

Клиент оставляет заявку на сайте, после чего она сохраняется в системе, а менеджер получает уведомление в Telegram. В закрытой административной части менеджер может просматривать и обрабатывать поступившие заявки.

## Возможности

Основной сценарий работы:

1. Клиент открывает лендинг и отправляет заявку.
2. Заявка сохраняется в PostgreSQL со статусом `Новая`.
3. Менеджер получает уведомление в Telegram.
4. Менеджер входит в закрытую административную часть.
5. Просматривает и фильтрует поступившие заявки.
6. Открывает карточку заявки.
7. Изменяет её статус и при необходимости добавляет внутренний комментарий.
8. Система сохраняет историю изменения статусов.

## Технологии

- .NET 10
- ASP.NET Core MVC
- Entity Framework Core
- PostgreSQL
- ASP.NET Core Identity
- Telegram Bot API
- Docker Compose
- xUnit
- WebApplicationFactory
- Testcontainers

## Документация проекта

- [AGENTS.md](AGENTS.md) — правила работы с репозиторием для агентов и разработчиков.
- [REQUIREMENTS.md](REQUIREMENTS.md) — бизнес-требования и границы MVP.
- [ARCHITECTURE.md](ARCHITECTURE.md) — архитектура, состав слоёв, зависимости и технические решения.
- [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) — этапы и задачи реализации.

## Локальный запуск

Все команды выполняются из корня репозитория. На Windows их можно вводить во встроенном терминале Visual Studio.

### Требования

- .NET SDK 10;
- Windows 10/11: Visual Studio и Docker Desktop в режиме Linux containers;
- Linux/macOS: Docker с поддержкой Docker Compose.

Проверьте установленные инструменты:

```text
dotnet --version
docker compose version
```

Установите локально зафиксированную для проекта версию Entity Framework CLI:

```text
dotnet tool restore
```

### Первый запуск

#### 1. Создайте файл `.env`

Windows, терминал Visual Studio:

```text
copy .env.example .env
```

Linux/macOS:

```bash
cp .env.example .env
```

Откройте созданный `.env` и задайте непустой уникальный пароль:

```dotenv
POSTGRES_PASSWORD=<локальный-пароль-базы-данных>
```

Не добавляйте `.env` в Git. Если пароль останется пустым, PostgreSQL не запустится.

`.env` используется Docker Compose для настройки PostgreSQL, а User Secrets — приложением LeadFlow. Поэтому пароль базы данных также указывается в строке подключения приложения на шаге 3.

#### 2. Запустите PostgreSQL

Перед выполнением следующей команды запустите Docker Desktop. На Linux убедитесь, что запущен Docker Engine.

```text
docker compose up -d
```

Дождитесь готовности контейнера:

```text
docker compose ps
```

У сервиса `postgres` должен появиться статус `healthy`.

#### 3. Настройте подключение приложения к БД

В следующей команде замените `<пароль-из-.env>` на значение `POSTGRES_PASSWORD` из файла `.env`:

```text
dotnet user-secrets set --project LeadFlow.Web/LeadFlow.Web.csproj "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=leadflow;Username=leadflow;Password=<пароль-из-.env>"
```

#### 4. Создайте учётные данные начального менеджера

Замените пример email и значение в угловых скобках на собственные данные:

```text
dotnet user-secrets set --project LeadFlow.Web/LeadFlow.Web.csproj "InitialManager:Email" "manager@example.com"
dotnet user-secrets set --project LeadFlow.Web/LeadFlow.Web.csproj "InitialManager:Password" "<пароль-менеджера>"
```

Пароль менеджера должен содержать не менее 12 символов, строчную и заглавную буквы, цифру и специальный символ. Формат `ManagerPass1!` соответствует требованиям, но для запуска используйте свой уникальный пароль.

#### 5. Примените миграции

```text
dotnet ef database update --project LeadFlow.Infrastructure/LeadFlow.Infrastructure.csproj --startup-project LeadFlow.Web/LeadFlow.Web.csproj
```

Команда создаст в PostgreSQL схему приложения и таблицы ASP.NET Core Identity.

#### 6. При необходимости настройте Telegram

Чтобы получать уведомления о новых заявках, задайте оба секрета:

```text
dotnet user-secrets set --project LeadFlow.Web/LeadFlow.Web.csproj "Telegram:BotToken" "<токен-бота>"
dotnet user-secrets set --project LeadFlow.Web/LeadFlow.Web.csproj "Telegram:ChatId" "<идентификатор-чата>"
```

Не сохраняйте реальные `BotToken` и `ChatId` в репозитории и не публикуйте их в открытом виде.

Без этих значений приложение также запускается и работает, но не отправляет Telegram-уведомления.

#### 7. Подготовьте локальный HTTPS-сертификат

На новой машине при необходимости подготовьте и доверьте локальный development-сертификат:

```text
dotnet dev-certs https --trust
```

На Windows может появиться системный запрос на подтверждение доверия сертификату.

#### 8. Запустите приложение

```text
dotnet run --project LeadFlow.Web/LeadFlow.Web.csproj --launch-profile https
```

Не закрывайте терминал, пока работает приложение. При первом запуске будет создан менеджер с email и паролем из шага 4.

После запуска приложение доступно по адресу `https://localhost:7120`.

- `https://localhost:7120` — лендинг и форма отправки заявки;
- `https://localhost:7120/admin/login` — вход в административную часть.

## Эксплуатация и устранение проблем

### Остановка приложения и PostgreSQL

Остановите приложение сочетанием `Ctrl+C` в терминале.

Контейнер PostgreSQL можно остановить без удаления данных:

```text
docker compose down
```

При следующем запуске достаточно снова выполнить:

```text
docker compose up -d
```

а затем запустить приложение.

Повторно применять миграции можно безопасно: EF Core выполнит только ещё не применённые миграции.

### Повторная инициализация PostgreSQL

Переменные `POSTGRES_DB`, `POSTGRES_USER` и `POSTGRES_PASSWORD` применяются только при первом создании volume PostgreSQL.

Изменение этих значений в `.env` не меняет учётные данные уже созданной базы.

Чтобы создать полностью новую локальную БД:

```text
docker compose down -v
docker compose up -d
```

Команда `docker compose down -v` необратимо удаляет все локальные данные PostgreSQL.

После пересоздания проверьте строку подключения и снова примените миграции.

### Порт PostgreSQL уже занят

Измените `POSTGRES_PORT` в `.env`, например на `5433`, и укажите тот же порт в `ConnectionStrings:DefaultConnection`:

```text
Host=localhost;Port=5433;Database=leadflow;Username=leadflow;Password=<пароль-из-.env>
```

### Проблемы с HTTPS-сертификатом

Повторно создайте и зарегистрируйте локальный сертификат:

```text
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

После этого перезапустите приложение и браузер.

### Отключение Telegram-уведомлений

```text
dotnet user-secrets remove --project LeadFlow.Web/LeadFlow.Web.csproj "Telegram:BotToken"
dotnet user-secrets remove --project LeadFlow.Web/LeadFlow.Web.csproj "Telegram:ChatId"
```

Ошибка Telegram не отменяет сохранение заявки. Причину ошибки следует искать в логах приложения, не публикуя токен и полное содержимое заявки.

### Альтернативная передача конфигурации

Вместо User Secrets можно использовать переменные окружения.

В именах ключей замените `:` на `__`, например:

```text
ConnectionStrings__DefaultConnection
InitialManager__Password
```

### Автоматические проверки

Для тестов Docker должен быть запущен: Testcontainers создаёт отдельный контейнер PostgreSQL и не обращается к реальному Telegram Bot API.

```text
dotnet build LeadFlow.sln --warnaserror
dotnet test LeadFlow.sln
```