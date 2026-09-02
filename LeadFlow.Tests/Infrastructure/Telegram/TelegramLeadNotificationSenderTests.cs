using System.Net;
using System.Text.Json;
using LeadFlow.Application.Notifications;
using LeadFlow.Infrastructure.Telegram;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LeadFlow.Tests.Infrastructure.Telegram;

public sealed class TelegramLeadNotificationSenderTests
{
    [Fact]
    public async Task SendNewLeadAsync_SendsExpectedPlainTextRequest()
    {
        var handler = new RecordingHttpMessageHandler(HttpStatusCode.OK);
        var sender = CreateTelegramSender(handler);

        await sender.SendNewLeadAsync(new NewLeadNotification(
            "Иван",
            "+7 999 123-45-67",
            "Разработка сайта",
            "Нужен лендинг."));

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("/bottest-token/sendMessage", handler.Request.RequestUri!.AbsolutePath);

        using var document = JsonDocument.Parse(handler.RequestBody!);
        var root = document.RootElement;
        Assert.Equal("123456", root.GetProperty("chat_id").GetString());
        Assert.Equal(
            "Новая заявка\n\nИмя: Иван\nТелефон: +7 999 123-45-67\nУслуга: Разработка сайта\n\nКомментарий:\nНужен лендинг.",
            root.GetProperty("text").GetString());
        Assert.False(root.TryGetProperty("parse_mode", out _));
    }

    [Fact]
    public async Task SendNewLeadAsync_WithFailedResponse_DoesNotThrow()
    {
        var sender = CreateTelegramSender(new RecordingHttpMessageHandler(HttpStatusCode.BadGateway));

        await sender.SendNewLeadAsync(new NewLeadNotification("Иван", "+7 999", "Консультация", null));
    }

    [Fact]
    public void FormatMessage_WithoutCustomerComment_DoesNotContainCommentSection()
    {
        var message = TelegramLeadNotificationSender.FormatMessage(
            new NewLeadNotification("Иван", "+7 999", "Консультация", null));

        Assert.Equal("Новая заявка\n\nИмя: Иван\nТелефон: +7 999\nУслуга: Консультация", message);
        Assert.DoesNotContain("Комментарий", message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisabledSender_DoesNotSendOrThrow()
    {
        var sender = new DisabledLeadNotificationSender();

        await sender.SendNewLeadAsync(new NewLeadNotification("Иван", "+7 999", "Консультация", null));
    }

    [Fact]
    public void AddLeadNotifications_WithoutCredentials_RegistersDisabledSender()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLeadNotifications(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.IsType<DisabledLeadNotificationSender>(provider.GetRequiredService<ILeadNotificationSender>());
    }

    [Fact]
    public async Task AddLeadNotifications_DoesNotEmitHttpClientLogsContainingRequestUri()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telegram:BotToken"] = "test-token",
                ["Telegram:ChatId"] = "123456"
            })
            .Build();
        var loggerProvider = new RecordingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(loggerProvider));
        services.AddLeadNotifications(configuration);
        services.PostConfigureAll<HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.PrimaryHandler = new RecordingHttpMessageHandler(HttpStatusCode.OK);
            });
        });

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ILeadNotificationSender>();

        await sender.SendNewLeadAsync(new NewLeadNotification("Иван", "+7 999", "Консультация", null));

        Assert.DoesNotContain(
            loggerProvider.Messages,
            message => message.Category.StartsWith("System.Net.Http.HttpClient", StringComparison.Ordinal));
    }

    private static TelegramLeadNotificationSender CreateTelegramSender(RecordingHttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.telegram.org/")
        };
        var options = Options.Create(new TelegramOptions
        {
            BotToken = "test-token",
            ChatId = "123456"
        });

        return new TelegramLeadNotificationSender(
            client,
            options,
            NullLogger<TelegramLeadNotificationSender>.Instance);
    }

    private sealed class RecordingHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(statusCode);
        }
    }

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        public List<(string Category, string Message)> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, Messages);

        public void Dispose()
        {
        }
    }

    private sealed class RecordingLogger(string category, List<(string Category, string Message)> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            messages.Add((category, formatter(state, exception)));
        }
    }
}
