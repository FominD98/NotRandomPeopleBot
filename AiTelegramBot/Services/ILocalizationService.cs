using AiTelegramBot.Models;

namespace AiTelegramBot.Services;

public interface ILocalizationService
{
    LocalizedStrings GetStrings(string languageCode);
    string GetAvailableLanguagesMessage(string currentLanguageCode);
}
