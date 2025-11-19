using AiTelegramBot.Models.Configuration;
using AiTelegramBot.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AiTelegramBot.Tests.Services;

public class ContentFilterServiceTests
{
    private readonly Mock<ILogger<ContentFilterService>> _loggerMock;
    private readonly ContentFilterSettings _settings;
    private readonly ContentFilterService _service;

    public ContentFilterServiceTests()
    {
        _loggerMock = new Mock<ILogger<ContentFilterService>>();
        _settings = new ContentFilterSettings
        {
            EnableFiltering = true,
            BlockedWords = new List<string> { "спам", "реклама", "бан" },
            WarnOnDetection = true
        };
        var options = Options.Create(_settings);
        _service = new ContentFilterService(options, _loggerMock.Object);
    }

    [Fact]
    public void ContainsBlockedContent_ShouldReturnTrue_WhenMessageContainsBlockedWord()
    {
        // Arrange
        string message = "Это сообщение содержит спам";

        // Act
        var result = _service.ContainsBlockedContent(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsBlockedContent_ShouldReturnFalse_WhenMessageIsClean()
    {
        // Arrange
        string message = "Это обычное сообщение";

        // Act
        var result = _service.ContainsBlockedContent(message);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsBlockedContent_ShouldBeCaseInsensitive()
    {
        // Arrange
        string message = "Это СПАМ сообщение";

        // Act
        var result = _service.ContainsBlockedContent(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsBlockedContent_ShouldReturnFalse_WhenFilteringDisabled()
    {
        // Arrange
        var disabledSettings = new ContentFilterSettings
        {
            EnableFiltering = false,
            BlockedWords = new List<string> { "спам" }
        };
        var options = Options.Create(disabledSettings);
        var service = new ContentFilterService(options, _loggerMock.Object);
        string message = "Это спам сообщение";

        // Act
        var result = service.ContainsBlockedContent(message);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetWarningMessage_ShouldReturnMessage_WhenWarnEnabled()
    {
        // Act
        var message = _service.GetWarningMessage();

        // Assert
        Assert.NotEmpty(message);
    }

    [Fact]
    public void GetWarningMessage_ShouldReturnEmpty_WhenWarnDisabled()
    {
        // Arrange
        var noWarnSettings = new ContentFilterSettings
        {
            WarnOnDetection = false
        };
        var options = Options.Create(noWarnSettings);
        var service = new ContentFilterService(options, _loggerMock.Object);

        // Act
        var message = service.GetWarningMessage();

        // Assert
        Assert.Empty(message);
    }
}
