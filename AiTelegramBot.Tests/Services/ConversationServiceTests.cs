using AiTelegramBot.Models.Configuration;
using AiTelegramBot.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AiTelegramBot.Tests.Services;

public class ConversationServiceTests
{
    private readonly Mock<ILogger<ConversationService>> _loggerMock;
    private readonly ConversationSettings _settings;
    private readonly ConversationService _service;

    public ConversationServiceTests()
    {
        _loggerMock = new Mock<ILogger<ConversationService>>();
        _settings = new ConversationSettings
        {
            MaxHistoryMessages = 10,
            SystemPrompt = "Test prompt"
        };
        var options = Options.Create(_settings);
        _service = new ConversationService(options, _loggerMock.Object);
    }

    [Fact]
    public void GetOrCreateContext_ShouldCreateNewContext_WhenUserIdIsNew()
    {
        // Arrange
        long userId = 123456;
        string userName = "TestUser";

        // Act
        var context = _service.GetOrCreateContext(userId, userName);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(userId, context.UserId);
        Assert.Equal(userName, context.UserName);
        Assert.Empty(context.Messages);
    }

    [Fact]
    public void GetOrCreateContext_ShouldReturnExistingContext_WhenUserIdExists()
    {
        // Arrange
        long userId = 123456;
        var firstContext = _service.GetOrCreateContext(userId, "User1");

        // Act
        var secondContext = _service.GetOrCreateContext(userId, "User2");

        // Assert
        Assert.Same(firstContext, secondContext);
        Assert.Equal("User1", secondContext.UserName);
    }

    [Fact]
    public void AddMessage_ShouldAddMessageToContext()
    {
        // Arrange
        long userId = 123456;
        string role = "user";
        string content = "Test message";

        // Act
        _service.AddMessage(userId, role, content);
        var context = _service.GetOrCreateContext(userId);

        // Assert
        Assert.Single(context.Messages);
        Assert.Equal(role, context.Messages[0].Role);
        Assert.Equal(content, context.Messages[0].Content);
    }

    [Fact]
    public void AddMessage_ShouldLimitHistorySize()
    {
        // Arrange
        long userId = 123456;
        int messagesToAdd = _settings.MaxHistoryMessages * 2 + 5;

        // Act
        for (int i = 0; i < messagesToAdd; i++)
        {
            _service.AddMessage(userId, "user", $"Message {i}");
        }
        var context = _service.GetOrCreateContext(userId);

        // Assert
        Assert.True(context.Messages.Count <= _settings.MaxHistoryMessages * 2);
    }

    [Fact]
    public void ResetContext_ShouldRemoveContext()
    {
        // Arrange
        long userId = 123456;
        _service.GetOrCreateContext(userId);
        _service.AddMessage(userId, "user", "Test");

        // Act
        _service.ResetContext(userId);
        var newContext = _service.GetOrCreateContext(userId);

        // Assert
        Assert.Empty(newContext.Messages);
    }

    [Fact]
    public void CleanupOldContexts_ShouldRemoveOldContexts()
    {
        // Arrange
        long userId1 = 123456;
        long userId2 = 789012;
        _service.GetOrCreateContext(userId1);
        _service.GetOrCreateContext(userId2);

        // Act
        _service.CleanupOldContexts(TimeSpan.FromMilliseconds(-1));
        var context1 = _service.GetOrCreateContext(userId1);
        var context2 = _service.GetOrCreateContext(userId2);

        // Assert - New contexts should be created after cleanup
        Assert.Empty(context1.Messages);
        Assert.Empty(context2.Messages);
    }
}
