namespace AiTelegramBot.Services;

public interface IAiServiceFactory
{
    IAiService CreateService(string provider);
}
