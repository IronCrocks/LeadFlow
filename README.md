# LeadFlow

LeadFlow — MVP для приёма заявок с лендинга и их обработки менеджером.

## Документация проекта

- [README.md](README.md) — обзор проекта, настройка окружения, запуск и проверки.
- [AGENTS.md](AGENTS.md) — правила работы с репозиторием для агентов и разработчиков.
- [REQUIREMENTS.md](REQUIREMENTS.md) — бизнес-требования и границы MVP.
- [ARCHITECTURE.md](ARCHITECTURE.md) — архитектура, состав слоёв, зависимости и технические решения.
- [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) — этапы и задачи реализации.

## Локальный запуск

Требования: .NET SDK 10, Docker Desktop с Docker Compose и доступная команда `dotnet ef`.

Если Entity Framework CLI ещё не установлена, установите её один раз:

```bash
dotnet tool install --global dotnet-ef
```

### PostgreSQL

1. Создайте локальный файл переменных:

   ```bash
   cp .env.example .env
   ```

2. В `.env` укажите уникальный пароль для локальной разработки в `POSTGRES_PASSWORD`. Значение в шаблоне намеренно пустое: `docker compose up` остановится, пока пароль не будет задан.

3. Запустите базу данных:

   ```bash
   docker compose up -d
   ```

4. Убедитесь, что контейнер готов принимать подключения:

   ```bash
   docker compose ps
   ```

   У сервиса `postgres` должен быть статус `healthy`.

Чтобы остановить контейнер без удаления данных, выполните:

```bash
docker compose down
```

Команда ниже удаляет контейнер **и все локальные данные PostgreSQL**. Используйте её только если данные больше не нужны:

```bash
docker compose down -v
```

### Повторная инициализация локальной БД

Переменные `POSTGRES_DB`, `POSTGRES_USER` и `POSTGRES_PASSWORD` применяются PostgreSQL только при первом запуске с пустым volume. Изменение `.env` после инициализации не изменит существующие учётные данные базы.

Если база была впервые запущена с временными или неверными значениями, перед началом реальной разработки её нужно пересоздать:

1. Остановить Compose и удалить локальный volume командой `docker compose down -v`.
2. Создать `.env` из `.env.example` и задать нужный локальный пароль.
3. Запустить `docker compose up -d`.

Первый шаг необратимо удаляет все данные локальной базы. Его следует выполнять только когда эти данные больше не нужны. Для текущей проверки Docker-окружения база была инициализирована временным паролем, поэтому этот сценарий потребуется выполнить перед использованием БД для разработки.

### Секреты приложения

Connection string не хранится в `appsettings*.json`. Все User Secrets задаются через `LeadFlow.Web` — это единственный запускаемый проект. После запуска PostgreSQL задайте connection string:

```bash
dotnet user-secrets set --project LeadFlow.Web/LeadFlow.Web.csproj \
  "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=leadflow;Username=leadflow;Password=<пароль-из-.env>"
```

В этом же хранилище задаются пароль начального менеджера и настройки Telegram. Их нельзя добавлять в репозиторий. Design-time factory EF из `LeadFlow.Infrastructure` читает это же хранилище только для выполнения команд миграций.

Вместо User Secrets можно использовать переменные окружения: замените `:` в ключах на `__`, например `ConnectionStrings__DefaultConnection` и `InitialManager__Password`.

### Начальный менеджер

Чтобы включить вход в административную часть, задайте email и пароль менеджера через User Secrets:

```bash
dotnet user-secrets set --project LeadFlow.Web/LeadFlow.Web.csproj "InitialManager:Email" "manager@example.com"
dotnet user-secrets set --project LeadFlow.Web/LeadFlow.Web.csproj "InitialManager:Password" "<надёжный-пароль-не-короче-12-символов>"
```

Пароль должен содержать не менее 12 символов, строчную и заглавную буквы, цифру и специальный символ. Например, формат `ManagerPass1!` соответствует этим правилам; для реального запуска используйте свой уникальный пароль.

При следующем запуске приложения менеджер будет создан в БД, если пользователя с таким email ещё нет. Не добавляйте эти значения в `appsettings*.json` или `.env.example`.

### Применение миграций

После настройки connection string примените схему к локальной PostgreSQL:

```bash
dotnet ef database update \
  --project LeadFlow.Infrastructure/LeadFlow.Infrastructure.csproj \
  --startup-project LeadFlow.Web/LeadFlow.Web.csproj
```

`LeadFlow.Infrastructure` содержит `ApplicationDbContext` и миграции, а `LeadFlow.Web` остаётся startup-проектом и источником конфигурации. Команда использует ту же строку подключения из User Secrets или environment variables. Если она не задана, миграция завершится с диагностичным сообщением, не раскрывая пароль.

### Запуск приложения

```bash
dotnet run --project LeadFlow.Web/LeadFlow.Web.csproj
```

После запуска откройте адрес, выведенный в консоль (обычно `https://localhost:xxxx`). Лендинг доступен по `/`, а вход менеджера — по `/admin/login`. При первом запуске после задания `InitialManager` создаётся пользователь менеджера.

### Telegram-уведомления

Чтобы включить уведомления о новых заявках, задайте оба значения через User Secrets:

```bash
dotnet user-secrets set --project LeadFlow.Web/LeadFlow.Web.csproj "Telegram:BotToken" "<токен-бота>"
dotnet user-secrets set --project LeadFlow.Web/LeadFlow.Web.csproj "Telegram:ChatId" "<идентификатор-чата>"
```

Если отсутствует хотя бы одно из значений, приложение работает без отправки уведомлений. Это не мешает запуску, созданию заявок и работе менеджера.

### Ручная Telegram smoke-проверка

Эта проверка выполняется только владельцем реальных секретов. Не передавайте `BotToken` и `ChatId` в чат, код или файлы репозитория.

1. Убедитесь, что PostgreSQL запущен, миграции применены, а `ConnectionStrings:DefaultConnection`, `InitialManager:Email` и `InitialManager:Password` заданы.
2. Задайте `Telegram:BotToken` и `Telegram:ChatId` через User Secrets командами выше.
3. Запустите приложение и отправьте на `/` новую заявку с именем, телефоном, услугой и непустым комментарием.
4. Убедитесь, что браузер перенаправлен на `/leads/success`, а в Telegram пришло одно plaintext-сообщение с этими полями.
5. Войдите через `/admin/login`, найдите заявку и проверьте её статус `Новая`, данные клиента и первую запись истории статусов.
6. Измените статус на `В работе`, сохраните карточку и убедитесь, что появилась вторая запись истории.

Для возврата к режиму без уведомлений удалите оба Telegram-значения из User Secrets:

```bash
dotnet user-secrets remove --project LeadFlow.Web/LeadFlow.Web.csproj "Telegram:BotToken"
dotnet user-secrets remove --project LeadFlow.Web/LeadFlow.Web.csproj "Telegram:ChatId"
```

Ошибка Telegram не должна отменить сохранение заявки: её нужно проверить по логам без вывода токена и без полного текста заявки.

### Автоматические проверки

```bash
dotnet build LeadFlow.sln --warnaserror
dotnet test LeadFlow.sln
```

Автоматические тесты используют отдельную PostgreSQL в Testcontainers и не вызывают реальный Telegram Bot API. Для их запуска Docker Desktop должен быть включён.
