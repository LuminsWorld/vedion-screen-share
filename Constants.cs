namespace VedionScreenShare;

public static class Constants
{
    // ── Firebase ──────────────────────────────────────────────────────────
    public const string FirebaseApiKey      = "AIzaSyCblDZsS9ybaifu2fcg5zzwGlEEaB_v5lw";
    public const string FirebaseProjectId   = "vedion-978cc";
    public const string FirebaseAuthDomain  = "vedion-978cc.firebaseapp.com";

    // Google OAuth Web Client ID
    // Get from: Firebase Console → Authentication → Sign-in method → Google → Web SDK configuration → Web client ID
    public const string GoogleClientId     = "597550884386-PLACEHOLDER.apps.googleusercontent.com";

    // ── App ───────────────────────────────────────────────────────────────
    public const string AppVersion         = "2.0.0";
    public const string AppName            = "Vedion Screen Share";
    public const string ConfigFileName     = "config.json";
    public const string AppDataFolder      = "VedionScreenShare";

    // ── GitHub (auto-update) ──────────────────────────────────────────────
    public const string GitHubOwner        = "LuminsWorld";
    public const string GitHubRepo         = "vedion-screen-share";
    public const string ReleasesApiUrl     = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";

    // ── Firebase REST endpoints ───────────────────────────────────────────
    public static string AuthSignInUrl     => $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={FirebaseApiKey}";
    public static string AuthSignUpUrl     => $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={FirebaseApiKey}";
    public static string AuthRefreshUrl    => $"https://securetoken.googleapis.com/v1/token?key={FirebaseApiKey}";
    public static string AuthLookupUrl     => $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={FirebaseApiKey}";
    public static string AuthIdpUrl        => $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={FirebaseApiKey}";
    public static string FirestoreBase     => $"https://firestore.googleapis.com/v1/projects/{FirebaseProjectId}/databases/(default)/documents";

    // ── AI Provider presets ───────────────────────────────────────────────
    public static readonly AiPreset[] AiPresets =
    {
        new("Gemini",    "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={key}", "gemini-2.0-flash", AiFormat.Gemini),
        new("OpenAI",    "https://api.openai.com/v1/chat/completions",  "gpt-4o",        AiFormat.OpenAi),
        new("Claude",    "https://api.anthropic.com/v1/messages",        "claude-3-5-haiku-20241022", AiFormat.Anthropic),
        new("Groq",      "https://api.groq.com/openai/v1/chat/completions", "meta-llama/llama-4-scout-17b-16e-instruct", AiFormat.OpenAi),
        new("Ollama",    "http://localhost:11434/v1/chat/completions",   "llava",         AiFormat.OpenAi),
        new("Custom",    "",                                              "",              AiFormat.OpenAi),
    };
}

public record AiPreset(string Name, string EndpointTemplate, string DefaultModel, AiFormat Format);

public enum AiFormat { OpenAi, Gemini, Anthropic }
