namespace LeadFlow.Infrastructure.Telegram;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string? BotToken { get; set; }

    public string? ChatId { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BotToken) &&
        !string.IsNullOrWhiteSpace(ChatId);
}
