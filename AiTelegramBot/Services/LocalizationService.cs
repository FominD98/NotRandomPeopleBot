using AiTelegramBot.Models;

namespace AiTelegramBot.Services;

public class LocalizationService : ILocalizationService
{
    private readonly Dictionary<string, LocalizedStrings> _translations = new();

    public LocalizationService()
    {
        InitializeTranslations();
    }

    public LocalizedStrings GetStrings(string languageCode)
    {
        return _translations.TryGetValue(languageCode, out var strings)
            ? strings
            : _translations["ru"]; // Default to Russian
    }

    public string GetAvailableLanguagesMessage(string currentLanguageCode)
    {
        var strings = GetStrings(currentLanguageCode);
        return strings.LanguageSelection;
    }

    private void InitializeTranslations()
    {
        // Russian translations - Электронный экскурсовод
        _translations["ru"] = new LocalizedStrings
        {
            GreetingHello = "Здравствуйте!",
            StartMessage = "Привет!\n\nЯ - электронный экскурсовод по Казани!\n\nИспользуйте /help, чтобы узнать команды.",
            StartMessageWithName = "Привет, {0}!\n\nЯ - электронный экскурсовод по Казани!\n\nИспользуйте /help для команд.",
            StartMessageDescription = "Я - электронный экскурсовод по Казани. Рассказываю об истории и достопримечательностях города.",
            StartMessageHelp = "Используйте /help для списка команд.",

            HelpMessage = "Доступные команды:\n\n",
            HelpCommandStart = "/start - Главное меню",
            HelpCommandHelp = "/help - Показать справку",
            HelpCommandAbout = "/about - О боте-экскурсоводе",
            HelpCommandReset = "/reset - Очистить историю диалога",
            HelpCommandLanguage = "/language - Выбрать язык (русский/татарский)",
            HelpCommandProvider = "/provider - Выбрать AI модель",
            HelpMessageFooter = "\n\n💡 Совет: используйте /tour для получения экскурсий!",

            AboutMessage = "🏛️ Электронный экскурсовод по Казани\n\n",
            AboutDescription = "Я помогу вам узнать больше о городе Казань и Республике Татарстан!\n\n",
            AboutCapabilities = "Мои возможности:\n",
            AboutCapability1 = "- Генерация аудио-экскурсий по геолокации",
            AboutCapability2 = "- Рассказы об истории и достопримечательностях",
            AboutCapability3 = "- Интересные факты о Казани",
            AboutCapability4 = "- Ответы на вопросы о городе",
            AboutCapability5 = "- Озвучка экскурсий через ElevenLabs",

            ResetConfirmation = "✅ История диалога очищена. Начинаем заново!",

            LanguageSelection = "Выберите язык / Тел сайлагыз:\n\n🇷🇺 Русский - /lang_ru\n🇹🇦 Татарский - /lang_tt",
            LanguageChanged = "Язык изменен на русский.",
            LanguageRussian = "Русский",
            LanguageTatar = "Татарский",

            ProviderSelection = "Выберите AI провайдера:\n\n🤖 DeepSeek - /provider_deepseek\n🧠 ChatGPT (OpenAI) - /provider_openai\n🇷🇺 Yandex GPT - /provider_yandex",
            ProviderChanged = "AI провайдер изменен на {0}.",
            ProviderDeepSeek = "DeepSeek",
            ProviderOpenAI = "ChatGPT (OpenAI)",
            ProviderYandexGpt = "Yandex GPT",

            UnknownCommand = "❓ Команда не распознана. Используйте /help для списка команд.",
            ErrorProcessing = "❌ Произошла ошибка при обработке запроса. Попробуйте позже."
        };

        // Tatar translations - Электрон экскурсовод
        _translations["tt"] = new LocalizedStrings
        {
            GreetingHello = "Исәнмесез!",
            StartMessage = "Сәлам!\n\nМин Казан буенча электрон экскурсовод!\n\n/help командасын кулланып белешмә алыгыз.",
            StartMessageWithName = "Сәлам, {0}!\n\nМин Казан буенча электрон экскурсовод!\n\n/help командасын кулланыгыз.",
            StartMessageDescription = "Мин Казан буенча электрон экскурсовод. Шәһәр тарихы һәм күренекле урыннары турында сөйлим.",
            StartMessageHelp = "/help командасын кулланыгыз.",

            HelpMessage = "Мөмкин булган командалар:\n\n",
            HelpCommandStart = "/start - Төп меню",
            HelpCommandHelp = "/help - Белешмәне күрсәтү",
            HelpCommandAbout = "/about - Бот-экскурсовод турында",
            HelpCommandReset = "/reset - Диалог тарихын чистарту",
            HelpCommandLanguage = "/language - Тел сайлау (русча/татарча)",
            HelpCommandProvider = "/provider - AI моделен сайлау",
            HelpMessageFooter = "\n\n💡 Киңәш: экскурсияләр алу өчен /tour кулланыгыз!",

            AboutMessage = "🏛️ Казан буенча электрон экскурсовод\n\n",
            AboutDescription = "Мин сезгә Казан шәһәре һәм Татарстан Республикасы турында күбрәк белергә булышам!\n\n",
            AboutCapabilities = "Минем мөмкинлекләрем:\n",
            AboutCapability1 = "- Геолокация буенча аудио-экскурсияләр ясау",
            AboutCapability2 = "- Тарих һәм күренекле урыннар турында хикәяләр",
            AboutCapability3 = "- Казан турында кызыклы фактлар",
            AboutCapability4 = "- Шәһәр турында сорауларга җаваплар",
            AboutCapability5 = "- ElevenLabs аша экскурсияләрне тавышландыру",

            ResetConfirmation = "✅ Диалог тарихы чистартылды. Яңадан башлыйбыз!",

            LanguageSelection = "Выберите язык / Тел сайлагыз:\n\n🇷🇺 Русский - /lang_ru\n🇹🇦 Татарча - /lang_tt",
            LanguageChanged = "Тел үзгәртелде.",
            LanguageRussian = "Русча",
            LanguageTatar = "Татарча",

            ProviderSelection = "AI провайдерны сайлагыз:\n\n🤖 DeepSeek - /provider_deepseek\n🧠 ChatGPT (OpenAI) - /provider_openai\n🇷🇺 Yandex GPT - /provider_yandex",
            ProviderChanged = "AI провайдер {0} га үзгәртелде.",
            ProviderDeepSeek = "DeepSeek",
            ProviderOpenAI = "ChatGPT (OpenAI)",
            ProviderYandexGpt = "Yandex GPT",

            UnknownCommand = "❓ Команда танылмады. Командалар исемлеге өчен /help кулланыгыз.",
            ErrorProcessing = "❌ Сорауны эшкәртүдә хата килеп чыкты. Соңрак кабатлап карагыз."
        };
    }
}
