# LeadFlow

## Локальная разработка

Требования: .NET SDK 10 и Docker Desktop с Docker Compose.

### PostgreSQL

1. Создайте локальный файл переменных:

   ```bash
   cp .env.example .env
   ```

2. В `.env` замените `POSTGRES_PASSWORD` на уникальный пароль для локальной разработки.

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

Connection string не хранится в `appsettings*.json`. После запуска PostgreSQL задайте его через User Secrets:

```bash
dotnet user-secrets set --project LeadFlow/LeadFlow.csproj \
  "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=leadflow;Username=leadflow;Password=<пароль-из-.env>"
```

В этом же хранилище задаются пароль начального менеджера и настройки Telegram. Их нельзя добавлять в репозиторий.

### Начальный менеджер

Чтобы включить вход в административную часть, задайте email и пароль менеджера через User Secrets:

```bash
dotnet user-secrets set --project LeadFlow/LeadFlow.csproj "InitialManager:Email" "manager@example.com"
dotnet user-secrets set --project LeadFlow/LeadFlow.csproj "InitialManager:Password" "<надёжный-пароль-не-короче-12-символов>"
```

При следующем запуске приложения менеджер будет создан в БД, если пользователя с таким email ещё нет. Не добавляйте эти значения в `appsettings*.json` или `.env.example`.

### Telegram-уведомления

Чтобы включить уведомления о новых заявках, задайте оба значения через User Secrets:

```bash
dotnet user-secrets set --project LeadFlow/LeadFlow.csproj "Telegram:BotToken" "<токен-бота>"
dotnet user-secrets set --project LeadFlow/LeadFlow.csproj "Telegram:ChatId" "<идентификатор-чата>"
```

Если отсутствует хотя бы одно из значений, приложение работает без отправки уведомлений. После настройки запустите приложение, отправьте форму на лендинге и убедитесь, что в Telegram пришло сообщение с именем, телефоном, услугой и комментарием клиента.

### Проверка приложения

```bash
dotnet build LeadFlow.sln
dotnet test LeadFlow.sln
```
