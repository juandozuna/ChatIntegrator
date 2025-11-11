using System.Text.Json;
using System.Text.Json.Serialization;
using MentionSync.Application.Integrations;
using MentionSync.Domain.Entities;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace MentionSync.Infrastructure.Integrations;

public class OpenAiEnricher : IAiEnricher
{
    private readonly ChatClient _client;
    private readonly ILogger<OpenAiEnricher> _logger;

    public OpenAiEnricher(ChatClient client, ILogger<OpenAiEnricher> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<AiEnrichmentResult> EnrichAsync(SourceMessage message, string targetUserHandle, CancellationToken cancellationToken = default)
    {
        var prompt = $"""
You are MentionSync, an assistant that triages work chat mentions.
Given the message text and metadata below, respond with a JSON object containing priority (0-3), summary (<=3 sentences), confidence (0-1), and implicit flag.
Target handle: {targetUserHandle}
Message: {message.Text}
""";

        var response = await _client.CompleteChatAsync(
            [
                new SystemChatMessage("You classify urgency and summarize messages for MentionSync users."),
                new UserChatMessage(prompt)
            ],
            new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchema(
                    jsonSchemaFormat: new JsonSchemaFormat(
                        Name: "MentionSyncClassification",
                        Schema: BinaryData.FromString(JsonSerializer.Serialize(new
                        {
                            type = "object",
                            properties = new
                            {
                                priority = new { type = "integer", minimum = 0, maximum = 3 },
                                summary = new { type = "string" },
                                confidence = new { type = "number", minimum = 0, maximum = 1 },
                                isImplicit = new { type = "boolean" }
                            },
                            required = new[] { "priority", "summary", "confidence", "isImplicit" }
                        })),
                        Strict = true
                    )
                )
            },
            cancellationToken
        );

        try
        {
            var document = JsonDocument.Parse(response.Value.Content[0].Text);
            var root = document.RootElement;
            return new AiEnrichmentResult(
                root.GetProperty("priority").GetInt32(),
                root.GetProperty("summary").GetString() ?? string.Empty,
                root.GetProperty("confidence").GetSingle(),
                root.GetProperty("isImplicit").GetBoolean()
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response: {Response}", response.Value.Content[0].Text);
            return new AiEnrichmentResult(1, message.Text ?? string.Empty, 0.3f, false);
        }
    }
}
