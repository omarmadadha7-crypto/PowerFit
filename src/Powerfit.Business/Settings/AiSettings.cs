namespace Powerfit.Business.Settings;

/// <summary>Bound from the "Ai" section of appsettings.json.</summary>
public class AiSettings
{
    public const string SectionName = "Ai";

    /// <summary>Provider API key. Keep it in user-secrets / environment variables.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.anthropic.com/";
    public string AnthropicVersion { get; set; } = "2023-06-01";

    /// <summary>Stored in AIGenerationLog.Model (max 50 chars).</summary>
    public string Model { get; set; } = "claude-sonnet-5-5";

    public int MaxTokens { get; set; } = 8000;
    public int TimeoutSeconds { get; set; } = 180;

    /// <summary>Upper limits for the catalogs that are sent to the model (keeps the prompt small).</summary>
    public int MaxExercisesInPrompt { get; set; } = 200;
    public int MaxFoodsInPrompt { get; set; } = 200;

    /// <summary>Plan generations one user may start in a rolling 24 h window (0 = unlimited). Every call costs money.</summary>
    public int MaxGenerationsPerUserPerDay { get; set; } = 10;

    /// <summary>Plan generations all users together may start in a rolling 24 h window (0 = unlimited): a budget cap.</summary>
    public int MaxGenerationsPerDay { get; set; } = 500;
}
