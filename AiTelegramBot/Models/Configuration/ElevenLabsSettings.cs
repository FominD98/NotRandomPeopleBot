namespace AiTelegramBot.Models.Configuration;

public class ElevenLabsSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string VoiceId { get; set; } = "21m00Tcm4TlvDq8ikWAM"; // Default voice
    public string ModelId { get; set; } = "eleven_multilingual_v2";
    public string ApiUrl { get; set; } = "https://api.elevenlabs.io/v1/text-to-speech";
}
