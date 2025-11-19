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
        // Russian translations - Су Анасы стиль
        _translations["ru"] = new LocalizedStrings
        {
            GreetingHello = "Исәнме, балакаем",
            StartMessage = "Привет!\n\nЯ Су Анасы, дух воды. Я слушаю твои вопросы и шепчу ответы, как тихий прибой.\n\nИспользуй /help, чтобы узнать команды.",
            StartMessageWithName = "Привет, {0}!\n\nЯ Су Анасы, дух воды. Слушаю твои вопросы и шепчу ответы, как тихий прибой.\n\nИспользуй /help для команд.",
            StartMessageDescription = "Я Су Анасы, дух воды. Отвечаю коротко, загадочно, как шепот волн.",
            StartMessageHelp = "Используй /help для списка команд.",

            HelpMessage = "Доступные команды:\n\n",
            HelpCommandStart = "/start - Начать диалог с Су Анасой",
            HelpCommandHelp = "/help - Показать эту справку",
            HelpCommandAbout = "/about - Информация о Су Анасе",
            HelpCommandReset = "/reset - Очистить диалог",
            HelpCommandLanguage = "/language - Выбрать язык",
            HelpCommandProvider = "/provider - Выбрать AI провайдера",
            HelpMessageFooter = "\n\nПросто отправь мне сообщение, и я тихо отвечу.",

            AboutMessage = "Су Анасы Bot v1.0\n\n",
            AboutDescription = "Я — Су Анасы, дух воды из татарского фольклора. Отвечаю коротко и загадочно, не обсуждаю политику, насилие или деньги.\n\n",
            AboutCapabilities = "Я могу:\n",
            AboutCapability1 = "- Отвечать на вопросы мягко и образно",
            AboutCapability2 = "- Давать советы как маленькие легенды",
            AboutCapability3 = "- Подсказывать простыми словами",
            AboutCapability4 = "- Сохранять контекст для плавного диалога",
            AboutCapability5 = "- Избегать неприличного и нежелательного контента",

            ResetConfirmation = "Течение изменилось, диалог очищен. Начнем с чистого берега.",

            LanguageSelection = "Выберите язык / Тел сайлагыз:\n\n🇷🇺 Русский - /lang_ru\n🇹🇦 Татарский - /lang_tt",
            LanguageChanged = "Язык изменен на русский.",
            LanguageRussian = "Русский",
            LanguageTatar = "Татарский",

            ProviderSelection = "Выберите AI провайдера:\n\n🤖 DeepSeek - /provider_deepseek\n🧠 ChatGPT (OpenAI) - /provider_openai\n🇷🇺 Yandex GPT - /provider_yandex",
            ProviderChanged = "AI провайдер изменен на {0}.",
            ProviderDeepSeek = "DeepSeek",
            ProviderOpenAI = "ChatGPT (OpenAI)",
            ProviderYandexGpt = "Yandex GPT",

            UnknownCommand = "Шёпот волн не понял команду. Используй /help.",
            ErrorProcessing = "Произошёл тихий шторм, попробуй ещё раз позже."
        };

        // Tatar translations - Су Анасы стиль
        _translations["tt"] = new LocalizedStrings
        {
            GreetingHello = "Исәнме, балакаем",
            StartMessage = "Сәлам!\n\nМин Су Анасы, су рухы. Сезнең сорауларны тыңлыйм һәм тыныч дулкыннар кебек җавап бирәм.\n\n/help командасын кулланып белешмә алыгыз.",
            StartMessageWithName = "Сәлам, {0}!\n\nМин Су Анасы, су рухы. Сезнең сорауларны тыңлыйм һәм тыныч дулкыннар кебек җавап бирәм.\n\n/help командасын кулланыгыз.",
            StartMessageDescription = "Мин Су Анасы, су рухы. Кыскача, серле җаваплар бирәм, дулкын шепеленгән кебек.",
            StartMessageHelp = "/help командасын кулланыгыз.",

            HelpMessage = "Мөмкин булган командалар:\n\n",
            HelpCommandStart = "/start - Су Анасымен сөйләшүне башлау",
            HelpCommandHelp = "/help - Бу белешмәне күрсәтү",
            HelpCommandAbout = "/about - Су Анасы турында мәгълүмат",
            HelpCommandReset = "/reset - Диалогны чистарту",
            HelpCommandLanguage = "/language - Тел сайлау",
            HelpCommandProvider = "/provider - AI провайдерны сайлау",
            HelpMessageFooter = "\n\nМиңа гади генә хәбәр җибәрегез, һәм мин тыныч җавап бирәм.",

            AboutMessage = "Су Анасы Bot v1.0\n\n",
            AboutDescription = "Мин — Су Анасы, татар фольклорыннан су рухы. Кыскача һәм серле җавап бирәм, сәясәт, насилие яки акча турында сөйләшмим.\n\n",
            AboutCapabilities = "Мин түбәндәгеләрне эшли алам:\n",
            AboutCapability1 = "- Сорауларга йомшак һәм образлы җавап бирү",
            AboutCapability2 = "- Кечкенә риваять яки киңәш бирү",
            AboutCapability3 = "- Гади сүзләр белән аңлату",
            AboutCapability4 = "- Диалог контекстын саклау",
            AboutCapability5 = "- Яраксыз һәм теләктәшсез эчтәлектән саклану",

            ResetConfirmation = "Агым үзгәрде, диалог чистартылды. Яңа сулыш белән башлыйбыз.",

            LanguageSelection = "Выберите язык / Тел сайлагыз:\n\n🇷🇺 Русский - /lang_ru\n🇹🇦 Татарча - /lang_tt",
            LanguageChanged = "Тел үзгәртелде.",
            LanguageRussian = "Русча",
            LanguageTatar = "Татарча",

            ProviderSelection = "AI провайдерны сайлагыз:\n\n🤖 DeepSeek - /provider_deepseek\n🧠 ChatGPT (OpenAI) - /provider_openai\n🇷🇺 Yandex GPT - /provider_yandex",
            ProviderChanged = "AI провайдер {0} га үзгәртелде.",
            ProviderDeepSeek = "DeepSeek",
            ProviderOpenAI = "ChatGPT (OpenAI)",
            ProviderYandexGpt = "Yandex GPT",

            UnknownCommand = "Дулкыннар командагызны аңламады. /help кулланыгыз.",
            ErrorProcessing = "Тын дулкыннарда проблема килеп чыкты, соңрак кабатлап карагыз."
        };
    }
}
