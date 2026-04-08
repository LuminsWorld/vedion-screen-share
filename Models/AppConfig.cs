namespace VedionScreenShare.Models;

public class AppConfig
{
    // ── Mode ──────────────────────────────────────────────────────────────
    public AppMode Mode { get; set; } = AppMode.Discord;

    // ── Discord Snapshot ──────────────────────────────────────────────────
    public string DiscordWebhookUrl   { get; set; } = "";
    public bool   DiscordSendImage    { get; set; } = true;

    // ── AI Analysis ───────────────────────────────────────────────────────
    public string AiProviderName      { get; set; } = "Gemini";
    public string AiEndpointUrl       { get; set; } = "";
    public string AiApiKey            { get; set; } = "";
    public string AiModel             { get; set; } = "gemini-2.0-flash";
    public AiFormat AiFormat          { get; set; } = AiFormat.Gemini;
    public string AiPrompt            { get; set; } = "Describe what you see on this screen. Be concise and helpful.";
    public bool   AiSendImage         { get; set; } = true;
    public string AiOutputWebhookUrl  { get; set; } = "";
    public bool   AiUseInterval       { get; set; } = false;
    public int    AiIntervalSeconds   { get; set; } = 10;

    // ── Capture ───────────────────────────────────────────────────────────
    public CaptureRegion? Region      { get; set; }
    public int    JpegQuality         { get; set; } = 60;

    // ── Hotkeys ───────────────────────────────────────────────────────────
    public uint HotkeySnapMod         { get; set; } = 0x0006; // Ctrl+Shift
    public uint HotkeySnapKey         { get; set; } = 0x53;   // S
    public uint HotkeyPauseMod        { get; set; } = 0x0006;
    public uint HotkeyPauseKey        { get; set; } = 0x50;   // P

    // ── Auth (tokens — not password) ──────────────────────────────────────
    public string? IdToken            { get; set; }
    public string? RefreshToken       { get; set; }
    public string? Uid                { get; set; }
    public string? Email              { get; set; }
    public string? DisplayName        { get; set; }
}

public enum AppMode { Discord, AiAnalysis }

public class CaptureRegion
{
    public int X      { get; set; }
    public int Y      { get; set; }
    public int Width  { get; set; }
    public int Height { get; set; }
}
