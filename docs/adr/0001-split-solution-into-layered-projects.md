# ADR-0001: Разделить решение на проекты по архитектурным слоям

- Статус: Accepted
- Дата: 2026-09-06
- Связанный документ: `ARCHITECTURE.md`

## Контекст

MVP LeadFlow реализован как модульный монолит в одном ASP.NET Core MVC-проекте. Логические границы Presentation, Application, Domain и Infrastructure поддерживаются каталогами и пространствами имён.

После завершения MVP требуется сделать эти границы явными на уровне сборок, сохранить существующие прикладные сервисы и подготовить решение к дальнейшему развитию. При этом нет требования менять пользовательское поведение, схему данных, способ развёртывания или best-effort модель Telegram-уведомлений.

Разделение только каталогами не предотвращает случайную зависимость Domain или Application от Web и Infrastructure. Разделение на проекты позволяет контролировать эти зависимости через project references и проверять их компилятором.

## Решение

Разделить production-код на четыре проекта:

- `LeadFlow.Web` — ASP.NET Core MVC, controllers, Razor views, view models, Areas, статические файлы и composition root;
- `LeadFlow.Application` — прикладные сервисы, команды, query-модели, DTO и контракты внешних зависимостей;
- `LeadFlow.Domain` — сущности, enum и доменные правила, не зависящие от инфраструктуры;
- `LeadFlow.Infrastructure` — EF Core/PostgreSQL, Identity, Telegram и регистрации инфраструктурных зависимостей.

`LeadFlow.Tests` остаётся одним тестовым проектом. Unit- и integration-тесты разделяются каталогами; отдельные тестовые проекты могут появиться позднее, если различия в зависимостях или времени выполнения станут существенными.

Существующие `LeadCreationService`, `LeadManagementService` и query-сервисы сохраняются в `LeadFlow.Application`. Код внутри Application группируется по прикладным сценариям (`Creation`, `Listing`, `Management`), а не в общих корневых каталогах `Commands` и `DTO`.

`IApplicationDbContext` объявляется в `LeadFlow.Application`, а `ApplicationDbContext` реализуется в `LeadFlow.Infrastructure`. Application-сервисы продолжают использовать конкретные LINQ-запросы и EF Core как unit of work. Generic repository не вводится.

Identity изолируется дополнительными application-контрактами:

- `IManagerAuthenticationService` предоставляет Web операции входа и выхода без типов ASP.NET Core Identity;
- `IManagerDirectory` предоставляет Application только необходимые сведения о менеджерах по идентификаторам;
- реализации этих контрактов и `ApplicationUser : IdentityUser<Guid>` находятся в `LeadFlow.Infrastructure/Identity`.

`AccountController` зависит от `IManagerAuthenticationService`, а не от `SignInManager<ApplicationUser>`. Проверка `returnUrl` и формирование HTTP-ответа остаются в Web. Identity-реализация сохраняет текущее поведение password sign-in, lockout и cookie authentication.

`LeadStatusHistory` в Domain хранит только `ChangedByUserId` и не имеет навигационного свойства на `ApplicationUser`. EF Core настраивает внешний ключ к Identity-пользователю в Infrastructure без доменной зависимости. Email автора истории разрешается через `IManagerDirectory`.

Допустимые направления ссылок:

```text
LeadFlow.Domain         → внешние проекты решения отсутствуют
LeadFlow.Application    → LeadFlow.Domain
LeadFlow.Infrastructure → LeadFlow.Application, LeadFlow.Domain
LeadFlow.Web            → LeadFlow.Application, LeadFlow.Infrastructure; при необходимости LeadFlow.Domain
LeadFlow.Tests          → тестируемые проекты
```

Прямая ссылка `LeadFlow.Web` на `LeadFlow.Domain` допускается только для типов, являющихся частью presentation-контрактов, например `LeadStatus`. Controllers не обращаются к `ApplicationDbContext`, Telegram-клиенту, `ApplicationUser`, `SignInManager<ApplicationUser>` или Identity-хранилищу напрямую.

Запускаемым и развёртываемым приложением остаётся только `LeadFlow.Web`. PostgreSQL остаётся единой базой данных. Отдельный Worker, очередь и outbox не добавляются.

При переносе сохраняются:

- существующие HTTP routes и пользовательское поведение;
- схема PostgreSQL и история EF Core migrations;
- имена конфигурационных ключей и environment variables;
- текущий `UserSecretsId`, чтобы локальные секреты не пришлось создавать заново;
- best-effort отправка Telegram после сохранения заявки;
- один `ApplicationDbContext` для Identity и заявок.

## Рассмотренные альтернативы

### Оставить один production-проект

Это минимальный и достаточный вариант для текущего размера MVP. Он отклонён как целевое состояние, поскольку следующий этап проекта ставит задачу явно продемонстрировать и технически закрепить архитектурные границы.

### Объединить Domain и Application в `LeadFlow.Core`

Вариант уменьшает количество проектов, но не закрепляет независимость доменной модели от прикладных сценариев. Для небольшого решения он допустим, однако выбранное разделение лучше показывает назначение каждого слоя и направление зависимостей.

### Создать `LeadFlow.Web` и `LeadFlow.Worker`

Worker решает операционную задачу фонового выполнения, а не задачу организации исходного кода. Без очереди или outbox второй процесс не даёт полезной границы и усложняет запуск, тестирование и развёртывание. Этот вариант может быть пересмотрен при появлении требования гарантированной доставки уведомлений.

### Ввести generic repository

Generic repository скрыл бы возможности EF Core и усложнил существующие query-сценарии без дополнительной бизнес-ценности. Для инверсии зависимости достаточно `IApplicationDbContext`.

## Последствия

Положительные:

- компилятор контролирует направления зависимостей;
- инфраструктурные детали отделены от прикладных сценариев;
- Web и Domain не зависят от конкретной Identity-модели;
- application-сервисы остаются независимо тестируемыми;
- размещение кода по сценариям упрощает навигацию и развитие;
- приложение сохраняет простой запуск и развёртывание одним процессом.

Отрицательные и нейтральные:

- увеличивается количество `.csproj` и project references;
- design-time команды EF Core должны явно учитывать Infrastructure и Web;
- перенос затронет пространства имён, тестовые фабрики и пути в документации;
- `LeadFlow.Application` будет зависеть от EF Core abstractions ради LINQ и `IApplicationDbContext`;
- для изоляции Identity появляются два узких application-контракта и их инфраструктурные реализации;
- физическое разделение проектов само по себе не добавляет новой функциональности пользователю.

## Ограничения реализации

Модуляризация выполняется как структурный рефакторинг без одновременного изменения бизнес-поведения. Каждая задача переноса должна оставлять solution собираемым и завершаться тестами и независимым ревью.

Конкретная последовательность задач намеренно не фиксируется в ADR. После утверждения решения она добавляется отдельным этапом в `IMPLEMENTATION_PLAN.md`; ADR остаётся стабильной записью архитектурного решения, даже если порядок реализации изменится.
