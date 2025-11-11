using MentionSync.Application.Common;
using MentionSync.Application.Integrations;
using MentionSync.Application.Mentions;
using MentionSync.Infrastructure.Data;
using MentionSync.Infrastructure.Integrations;
using MentionSync.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddDbContext<MentionSyncDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("pg")));
builder.Services.AddDbContextFactory<MentionSyncDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("pg")));

builder.Services.AddScoped<IMentionService, MentionService>();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddSingleton<IMentionExtractor, MentionExtractor>();
builder.Services.AddScoped<SlackWebhookService>();
builder.Services.AddScoped<TeamsWebhookService>();
builder.Services.AddScoped<GoogleChatWebhookService>();
builder.Services.AddHostedService<DiscordGatewayWorker>();

builder.Services.AddSingleton<IAiEnricher>(sp =>
{
    var apiKey = configuration["OpenAI:ApiKey"];
    var endpoint = configuration["OpenAI:Endpoint"];
    var model = configuration.GetValue<string>("OpenAI:Model") ?? "gpt-4o-mini";
    var client = string.IsNullOrWhiteSpace(endpoint)
        ? new OpenAIClient(apiKey)
        : new OpenAIClient(new Uri(endpoint), new OpenAIClientOptions(apiKey));
    return new OpenAiEnricher(client.GetChatClient(model), sp.GetRequiredService<ILogger<OpenAiEnricher>>());
});

builder.Services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
builder.Services.AddTransient<INotifier, EmailNotifier>();

builder.Services.Configure<SlackOptions>(configuration.GetSection("Slack"));
builder.Services.Configure<GraphOptions>(configuration.GetSection("Graph"));
builder.Services.Configure<DiscordOptions>(configuration.GetSection("Discord"));

builder.Services.AddHttpClient("Graph", client =>
{
    client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0");
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = configuration["Auth:Authority"];
        options.Audience = configuration["Auth:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
