namespace AiTelegramBot.Services;

public interface IElevenLabsService
{
    Task<byte[]?> GenerateAudioAsync(string text);
}
