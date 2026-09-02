using System.Net;
using LeadFlow.Application.Leads;
using LeadFlow.Application.Notifications;
using LeadFlow.Infrastructure.Telegram;
using LeadFlow.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LeadFlow.Tests.Infrastructure.Telegram;

[Collection(PostgreSqlCollection.Name)]
public sealed class LeadCreationNotificationTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task CreateAsync_WithRepeatedSubmission_SendsOnlyOneNotification()
    {
        await using var context = database.CreateContext();
        var sender = new RecordingNotificationSender();
        var service = new LeadCreationService(context, TimeProvider.System, sender);
        var command = CreateCommand();

        await service.CreateAsync(command);
        await service.CreateAsync(command);

        Assert.Single(sender.Notifications);
    }

    [Fact]
    public async Task CreateAsync_WhenTelegramFails_PersistsLead()
    {
        await using var context = database.CreateContext();
        var telegramSender = new TelegramLeadNotificationSender(
            new HttpClient(new StatusCodeHandler(HttpStatusCode.BadGateway))
            {
                BaseAddress = new Uri("https://api.telegram.org/")
            },
            Options.Create(new TelegramOptions { BotToken = "test-token", ChatId = "123456" }),
            NullLogger<TelegramLeadNotificationSender>.Instance);
        var service = new LeadCreationService(context, TimeProvider.System, telegramSender);
        var command = CreateCommand();

        await service.CreateAsync(command);

        Assert.True(await context.Leads.AnyAsync(lead => lead.SubmissionId == command.SubmissionId));
    }

    private static CreateLeadCommand CreateCommand()
    {
        return new CreateLeadCommand(
            Guid.NewGuid(),
            "Иван",
            "+7 999 123-45-67",
            null,
            "Разработка сайта",
            "Нужен лендинг.");
    }

    private sealed class RecordingNotificationSender : ILeadNotificationSender
    {
        public List<NewLeadNotification> Notifications { get; } = [];

        public Task SendNewLeadAsync(NewLeadNotification notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class StatusCodeHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}
