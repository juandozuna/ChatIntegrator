namespace MentionSync.Domain;

public static class Networks
{
    public const string Slack = "slack";
    public const string Teams = "teams";
    public const string GoogleChat = "gchat";
    public const string Discord = "discord";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Slack, Teams, GoogleChat, Discord
    };
}
