namespace WhisperFlowLocal.Models;

public enum CleanupProvider { Local, Groq, OpenAI }

public class AppSettings
{
    public CleanupProvider CleanupProvider { get; set; } = CleanupProvider.Local;

    public string GroqApiKey  { get; set; } = string.Empty;
    public string GroqModel   { get; set; } = "llama-3.3-70b-versatile";

    public string OpenAiApiKey { get; set; } = string.Empty;
    public string OpenAiModel  { get; set; } = "gpt-4o-mini";

    public bool HasCompletedOnboarding { get; set; } = false;
}
