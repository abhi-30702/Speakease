namespace WhisperFlowLocal.Models;

public class AppSettings
{
    public string GroqApiKey { get; set; } = string.Empty;
    public string GroqModel { get; set; } = "llama-3.3-70b-versatile";
    public bool HasCompletedOnboarding { get; set; } = false;
}
