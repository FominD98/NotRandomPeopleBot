using System.Collections.Concurrent;
using AiTelegramBot.Models;
using AiTelegramBot.Models.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiTelegramBot.Services;

public class ConversationService : IConversationService
{
    private readonly ConcurrentDictionary<long, ConversationContext> _contexts = new();
    private readonly ConversationSettings _settings;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        IOptions<ConversationSettings> settings,
        ILogger<ConversationService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public ConversationContext GetOrCreateContext(long userId, string? userName = null)
    {
        return _contexts.GetOrAdd(userId, id => new ConversationContext
        {
            UserId = id,
            UserName = userName
        });
    }

    public void AddMessage(long userId, string role, string content)
    {
        var context = GetOrCreateContext(userId);
        context.Messages.Add(new Message
        {
            Role = role,
            Content = content,
            Timestamp = DateTime.UtcNow
        });

        // Keep only recent messages
        if (context.Messages.Count > _settings.MaxHistoryMessages * 2)
        {
            var toRemove = context.Messages.Count - _settings.MaxHistoryMessages;
            context.Messages.RemoveRange(0, toRemove);
        }

        context.LastInteraction = DateTime.UtcNow;
        _logger.LogDebug("Added {Role} message for user {UserId}", role, userId);
    }

    public void ResetContext(long userId)
    {
        if (_contexts.TryRemove(userId, out var context))
        {
            _logger.LogInformation("Reset conversation context for user {UserId}", userId);
        }
    }

    public void CleanupOldContexts(TimeSpan maxAge)
    {
        var cutoffTime = DateTime.UtcNow - maxAge;
        var toRemove = _contexts
            .Where(kvp => kvp.Value.LastInteraction < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var userId in toRemove)
        {
            _contexts.TryRemove(userId, out _);
            _logger.LogInformation("Cleaned up old context for user {UserId}", userId);
        }
    }

    public void SetUserLanguage(long userId, string languageCode)
    {
        var context = GetOrCreateContext(userId);
        context.Language = languageCode;
        _logger.LogInformation("User {UserId} changed language to {Language}", userId, languageCode);
    }

    public string GetUserLanguage(long userId)
    {
        var context = GetOrCreateContext(userId);
        return context.Language;
    }

    public void SetUserAiProvider(long userId, string provider)
    {
        var context = GetOrCreateContext(userId);
        context.AiProvider = provider;
        _logger.LogInformation("User {UserId} changed AI provider to {Provider}", userId, provider);
    }

    public string GetUserAiProvider(long userId)
    {
        var context = GetOrCreateContext(userId);
        return context.AiProvider;
    }
}
