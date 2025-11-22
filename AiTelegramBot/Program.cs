using AiTelegramBot.Handlers;
using AiTelegramBot.Logging;
using AiTelegramBot.Models.Configuration;
using AiTelegramBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

// Configure Serilog
var loggingSettings = configuration.GetSection("Logging").Get<LoggingSettings>() ?? new LoggingSettings();

var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.WithProperty("service", loggingSettings.ServiceName)
    .Enrich.WithProperty("environment", loggingSettings.DeploymentEnvironment)
    .WriteTo.Console();

// Add Loki sink if configured
if (!string.IsNullOrEmpty(loggingSettings.LokiEndpoint) &&
    !string.IsNullOrEmpty(loggingSettings.GrafanaCloudAccessToken))
{
    loggerConfiguration.WriteTo.Http(
        requestUri: loggingSettings.LokiEndpoint,
        queueLimitBytes: 1_000_000,
        httpClient: new LokiHttpClient("1298563", loggingSettings.GrafanaCloudAccessToken),
        batchFormatter: new LokiFormatter(loggingSettings.ServiceName, loggingSettings.DeploymentEnvironment));
}

Log.Logger = loggerConfiguration.CreateLogger();

try
{
    Log.Information("Starting AI Telegram Bot application");

    // Build host with dependency injection
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            // Register configuration sections
            services.Configure<TelegramSettings>(configuration.GetSection("Telegram"));
            services.Configure<AiProviderSettings>(configuration.GetSection("AiProvider"));
            services.Configure<DeepSeekSettings>(configuration.GetSection("DeepSeek"));
            services.Configure<OpenAISettings>(configuration.GetSection("OpenAI"));
            services.Configure<YandexGptSettings>(configuration.GetSection("YandexGpt"));
            services.Configure<YandexGeocodingSettings>(configuration.GetSection("YandexGeocoding"));
            services.Configure<ElevenLabsSettings>(configuration.GetSection("ElevenLabs"));
            services.Configure<ConversationSettings>(configuration.GetSection("Conversation"));
            services.Configure<ContentFilterSettings>(configuration.GetSection("ContentFilter"));
            services.Configure<LoggingSettings>(configuration.GetSection("Logging"));

            // Register HttpClientFactory for AI services
            services.AddHttpClient();

            // Register AI service factory
            services.AddSingleton<IAiServiceFactory, AiServiceFactory>();

            // Register services
            services.AddSingleton<IConversationService, ConversationService>();
            services.AddSingleton<IContentFilterService, ContentFilterService>();
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<IYandexGeocodingService, YandexGeocodingService>();
            services.AddSingleton<IElevenLabsService, ElevenLabsService>();
            services.AddSingleton<YandexGptService>();
            services.AddSingleton<ITourGuideService, TourGuideService>();
            services.AddSingleton<CommandHandler>();
            services.AddSingleton<MessageHandler>();

            // Register Telegram Bot Client
            services.AddSingleton<ITelegramBotClient>(sp =>
            {
                var telegramSettings = sp.GetRequiredService<IOptions<TelegramSettings>>().Value;
                return new TelegramBotClient(telegramSettings.BotToken);
            });

            // Add logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });
        })
        .Build();

    // Get services from DI container
    var serviceProvider = host.Services;
    var botClient = serviceProvider.GetRequiredService<ITelegramBotClient>();
    var commandHandler = serviceProvider.GetRequiredService<CommandHandler>();
    var messageHandler = serviceProvider.GetRequiredService<MessageHandler>();
    var conversationService = serviceProvider.GetRequiredService<IConversationService>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    // Setup cancellation token
    using var cts = new CancellationTokenSource();

    // Configure update handler
    var receiverOptions = new ReceiverOptions
    {
        AllowedUpdates = new[] { UpdateType.Message }
    };

    botClient.StartReceiving(
        updateHandler: async (client, update, cancellationToken) =>
        {
            try
            {
                if (update.Message is not { } message)
                    return;

                // Handle location messages
                if (message.Type == MessageType.Location)
                {
                    await messageHandler.HandleLocationMessageAsync(client, message);
                    return;
                }

                if (message.Type != MessageType.Text)
                    return;

                // Handle commands
                if (message.Text?.StartsWith("/") == true)
                {
                    await commandHandler.HandleCommandAsync(client, message);
                }
                else
                {
                    // Handle regular messages
                    await messageHandler.HandleTextMessageAsync(client, message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling update");
            }
        },
        pollingErrorHandler: (client, exception, cancellationToken) =>
        {
            logger.LogError(exception, "Telegram API polling error");
            return Task.CompletedTask;
        },
        receiverOptions: receiverOptions,
        cancellationToken: cts.Token
    );

    var botInfo = await botClient.GetMeAsync(cts.Token);
    logger.LogInformation("Bot started: @{BotUsername}", botInfo.Username);
    Console.WriteLine($"Bot started: @{botInfo.Username}");
    Console.WriteLine("Press Ctrl+C to stop...");

    // Setup cleanup timer for old conversations
    var cleanupTimer = new Timer(
        callback: _ => conversationService.CleanupOldContexts(TimeSpan.FromHours(24)),
        state: null,
        dueTime: TimeSpan.FromMinutes(30),
        period: TimeSpan.FromMinutes(30)
    );

    // Wait for cancellation
    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    // Keep the application running
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    Log.Information("Bot stopped");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
