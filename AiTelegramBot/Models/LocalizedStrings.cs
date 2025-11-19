namespace AiTelegramBot.Models;

public class LocalizedStrings
{
    // Command responses
    public string StartMessage { get; set; } = string.Empty;
    public string StartMessageWithName { get; set; } = string.Empty;
    public string StartMessageDescription { get; set; } = string.Empty;
    public string StartMessageHelp { get; set; } = string.Empty;

    public string HelpMessage { get; set; } = string.Empty;
    public string HelpCommandStart { get; set; } = string.Empty;
    public string HelpCommandHelp { get; set; } = string.Empty;
    public string HelpCommandAbout { get; set; } = string.Empty;
    public string HelpCommandReset { get; set; } = string.Empty;
    public string HelpCommandLanguage { get; set; } = string.Empty;
    public string HelpMessageFooter { get; set; } = string.Empty;

    public string AboutMessage { get; set; } = string.Empty;
    public string AboutDescription { get; set; } = string.Empty;
    public string AboutCapabilities { get; set; } = string.Empty;
    public string AboutCapability1 { get; set; } = string.Empty;
    public string AboutCapability2 { get; set; } = string.Empty;
    public string AboutCapability3 { get; set; } = string.Empty;
    public string AboutCapability4 { get; set; } = string.Empty;
    public string AboutCapability5 { get; set; } = string.Empty;

    public string ResetConfirmation { get; set; } = string.Empty;

    public string LanguageSelection { get; set; } = string.Empty;
    public string LanguageChanged { get; set; } = string.Empty;
    public string LanguageRussian { get; set; } = string.Empty;
    public string LanguageTatar { get; set; } = string.Empty;

    public string ProviderSelection { get; set; } = string.Empty;
    public string ProviderChanged { get; set; } = string.Empty;
    public string ProviderDeepSeek { get; set; } = string.Empty;
    public string ProviderOpenAI { get; set; } = string.Empty;
    public string ProviderYandexGpt { get; set; } = string.Empty;
    public string HelpCommandProvider { get; set; } = string.Empty;

    public string UnknownCommand { get; set; } = string.Empty;
    public string ErrorProcessing { get; set; } = string.Empty;

    public string GreetingHello { get; set; } = string.Empty;
}
