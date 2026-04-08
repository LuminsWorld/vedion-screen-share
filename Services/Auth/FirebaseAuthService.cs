using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VedionScreenShare.Services.Auth;

public class FirebaseUser
{
    public string IdToken      { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public string Uid          { get; init; } = "";
    public string Email        { get; init; } = "";
    public string DisplayName  { get; init; } = "";
}

public class FirebaseAuthService
{
    private static readonly HttpClient _http = new();

    // ── Email / Password ──────────────────────────────────────────────────

    public static async Task<FirebaseUser> SignInAsync(string email, string password)
    {
        var body = JsonSerializer.Serialize(new { email, password, returnSecureToken = true });
        var resp = await _http.PostAsync(Constants.AuthSignInUrl,
            new StringContent(body, Encoding.UTF8, "application/json"));
        return await ParseAuthResponse(resp);
    }

    public static async Task<FirebaseUser> SignUpAsync(string email, string password, string displayName = "")
    {
        var body = JsonSerializer.Serialize(new { email, password, returnSecureToken = true });
        var resp = await _http.PostAsync(Constants.AuthSignUpUrl,
            new StringContent(body, Encoding.UTF8, "application/json"));
        var user = await ParseAuthResponse(resp);

        // Set display name if provided
        if (!string.IsNullOrWhiteSpace(displayName))
            await UpdateProfileAsync(user.IdToken, displayName);

        return user;
    }

    public static async Task<FirebaseUser> RefreshTokenAsync(string refreshToken)
    {
        var body = $"grant_type=refresh_token&refresh_token={Uri.EscapeDataString(refreshToken)}";
        var resp = await _http.PostAsync(Constants.AuthRefreshUrl,
            new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded"));

        var json = await resp.Content.ReadAsStringAsync();
        var doc  = JsonDocument.Parse(json);

        if (!resp.IsSuccessStatusCode)
            throw new Exception(ExtractError(doc) ?? "Token refresh failed");

        var root = doc.RootElement;
        var newIdToken  = root.GetProperty("id_token").GetString() ?? "";
        var newRefToken = root.GetProperty("refresh_token").GetString() ?? "";
        var uid         = root.GetProperty("user_id").GetString() ?? "";

        // Fetch user info for email/name
        var info = await GetUserInfoAsync(newIdToken);
        return new FirebaseUser
        {
            IdToken      = newIdToken,
            RefreshToken = newRefToken,
            Uid          = uid,
            Email        = info.email,
            DisplayName  = info.displayName,
        };
    }

    // ── Google OAuth (browser redirect) ───────────────────────────────────

    public static async Task<FirebaseUser> SignInWithGoogleAsync()
    {
        // Pick a random port for the local redirect listener
        int port = new Random().Next(49152, 65535);
        string redirectUri = $"http://localhost:{port}/callback";
        string state       = Guid.NewGuid().ToString("N");

        // Build Google OAuth URL
        string authUrl =
            $"https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={Uri.EscapeDataString(Constants.GoogleClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString("openid email profile")}" +
            $"&state={state}" +
            $"&prompt=select_account";

        // Open browser
        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

        // Listen for callback
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        var context  = await listener.GetContextAsync();
        var query    = context.Request.QueryString;
        string code  = query["code"] ?? throw new Exception("Google sign-in cancelled.");
        string retState = query["state"] ?? "";

        // Send a close page to the browser
        var closeHtml = "<html><body style='background:#0a0a0a;color:#00FF41;font-family:monospace;display:flex;align-items:center;justify-content:center;height:100vh'><h2>✓ Signed in. You can close this tab.</h2></body></html>";
        var buffer    = Encoding.UTF8.GetBytes(closeHtml);
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer);
        context.Response.Close();
        listener.Stop();

        if (retState != state) throw new Exception("OAuth state mismatch — possible CSRF.");

        // Exchange code for Google ID token using Firebase's token endpoint
        var tokenBody = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"]          = code,
            ["client_id"]     = Constants.GoogleClientId,
            ["redirect_uri"]  = redirectUri,
            ["grant_type"]    = "authorization_code",
        });

        var tokenResp = await _http.PostAsync("https://oauth2.googleapis.com/token", tokenBody);
        var tokenJson = await tokenResp.Content.ReadAsStringAsync();
        var tokenDoc  = JsonDocument.Parse(tokenJson);

        if (!tokenResp.IsSuccessStatusCode)
            throw new Exception("Failed to exchange Google auth code.");

        string googleIdToken = tokenDoc.RootElement.GetProperty("id_token").GetString()
            ?? throw new Exception("No ID token from Google.");

        // Sign into Firebase with the Google ID token
        var idpBody = JsonSerializer.Serialize(new
        {
            requestUri       = redirectUri,
            postBody         = $"id_token={Uri.EscapeDataString(googleIdToken)}&providerId=google.com",
            returnSecureToken = true,
            returnIdpCredential = true,
        });

        var idpResp = await _http.PostAsync(Constants.AuthIdpUrl,
            new StringContent(idpBody, Encoding.UTF8, "application/json"));
        return await ParseAuthResponse(idpResp);
    }

    // ── Password reset ────────────────────────────────────────────────────

    public static async Task SendPasswordResetAsync(string email)
    {
        var body = JsonSerializer.Serialize(new { requestType = "PASSWORD_RESET", email });
        await _http.PostAsync(
            $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={Constants.FirebaseApiKey}",
            new StringContent(body, Encoding.UTF8, "application/json"));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static async Task<(string email, string displayName)> GetUserInfoAsync(string idToken)
    {
        var body = JsonSerializer.Serialize(new { idToken });
        var resp = await _http.PostAsync(Constants.AuthLookupUrl,
            new StringContent(body, Encoding.UTF8, "application/json"));
        var json = await resp.Content.ReadAsStringAsync();
        var doc  = JsonDocument.Parse(json);
        var user = doc.RootElement.GetProperty("users")[0];
        return (
            user.TryGetProperty("email",       out var e) ? e.GetString() ?? "" : "",
            user.TryGetProperty("displayName", out var d) ? d.GetString() ?? "" : ""
        );
    }

    private static async Task UpdateProfileAsync(string idToken, string displayName)
    {
        var body = JsonSerializer.Serialize(new { idToken, displayName, returnSecureToken = false });
        await _http.PostAsync(
            $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={Constants.FirebaseApiKey}",
            new StringContent(body, Encoding.UTF8, "application/json"));
    }

    private static async Task<FirebaseUser> ParseAuthResponse(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        var doc  = JsonDocument.Parse(json);

        if (!resp.IsSuccessStatusCode)
            throw new Exception(ExtractError(doc) ?? "Authentication failed.");

        var root = doc.RootElement;
        return new FirebaseUser
        {
            IdToken      = root.TryGetProperty("idToken",      out var t) ? t.GetString() ?? "" : "",
            RefreshToken = root.TryGetProperty("refreshToken", out var r) ? r.GetString() ?? "" : "",
            Uid          = root.TryGetProperty("localId",      out var u) ? u.GetString() ?? "" : "",
            Email        = root.TryGetProperty("email",        out var e) ? e.GetString() ?? "" : "",
            DisplayName  = root.TryGetProperty("displayName",  out var d) ? d.GetString() ?? "" : "",
        };
    }

    private static string? ExtractError(JsonDocument doc)
    {
        if (doc.RootElement.TryGetProperty("error", out var err) &&
            err.TryGetProperty("message", out var msg))
            return msg.GetString()?.Replace("_", " ");
        return null;
    }
}
