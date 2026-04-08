using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using VedionScreenShare.Models;
using VedionScreenShare.Services;
using VedionScreenShare.Services.Auth;

namespace VedionScreenShare.Windows;

public partial class MainWindow : Window
{
    private AppConfig          _config;
    private readonly FirebaseUser _user;
    private HotkeyService?     _hotkeys;
    private System.Timers.Timer? _intervalTimer;
    private int                _snapCount;
    private AppMode            _currentMode;
    private System.Windows.Forms.NotifyIcon? _tray;

    public MainWindow(FirebaseUser user, AppConfig config)
    {
        InitializeComponent();
        _user   = user;
        _config = config;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        SetupTray();
        PopulateForm();
        SetMode(_config.Mode);
        RegisterHotkeys();

        string display = _user.DisplayName is { Length: > 0 } n ? n : _user.Email;
        UserLabel.Text = display.Length > 20 ? display[..20] + "…" : display;
    }

    // Custom chrome
    private void DragBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove();
    }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
    {
        SaveConfig();
        Hide();
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        SaveConfig();
        _hotkeys?.Dispose();
        _intervalTimer?.Stop();
        if (_tray is not null) _tray.Visible = false;
        Environment.Exit(0);
    }

    // ── Mode switching ────────────────────────────────────────────────────

    private void SetMode(AppMode mode)
    {
        _currentMode = mode;
        _config.Mode = mode;

        DiscordPanel.Visibility = mode == AppMode.Discord ? Visibility.Visible : Visibility.Collapsed;
        AiPanel.Visibility      = mode == AppMode.AiAnalysis ? Visibility.Visible : Visibility.Collapsed;

        // Discord tab active state
        bool disc = mode == AppMode.Discord;
        DiscordTab.Background   = disc
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x14, 0x00, 0xFF, 0x41))
            : System.Windows.Media.Brushes.Transparent;
        DiscordTabText.Foreground = disc
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0x41))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x44, 0x44, 0x44));
        DiscordDot.Fill = disc
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0x41))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x33, 0x33, 0x33));

        // AI tab active state
        bool ai = mode == AppMode.AiAnalysis;
        AiTab.Background   = ai
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x14, 0x7B, 0x2F, 0xFF))
            : System.Windows.Media.Brushes.Transparent;
        AiTabText.Foreground = ai
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x7B, 0x2F, 0xFF))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x44, 0x44, 0x44));
        AiDot.Fill = ai
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x7B, 0x2F, 0xFF))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x33, 0x33, 0x33));
    }

    private void TabDiscord_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => SetMode(AppMode.Discord);
    private void TabAi_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => SetMode(AppMode.AiAnalysis);
    private void HotkeyCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { HotkeyRadio.IsChecked = true; }
    private void IntervalCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { IntervalRadio.IsChecked = true; }

    // ── Form population ───────────────────────────────────────────────────

    private void PopulateForm()
    {
        // Discord
        DiscordWebhookInput.Text = _config.DiscordWebhookUrl;
        DiscordImageToggle.IsChecked = _config.DiscordSendImage;

        // AI
        foreach (var p in Constants.AiPresets)
            AiProviderCombo.Items.Add(p.Name);

        int idx = Constants.AiPresets.ToList().FindIndex(p => p.Name == _config.AiProviderName);
        AiProviderCombo.SelectedIndex = idx >= 0 ? idx : 0;

        AiKeyInput.Password    = _config.AiApiKey;
        AiModelInput.Text      = _config.AiModel;
        AiEndpointInput.Text   = _config.AiEndpointUrl;
        AiPromptInput.Text     = _config.AiPrompt;
        AiWebhookInput.Text    = _config.AiOutputWebhookUrl;
        AiImageToggle.IsChecked = _config.AiSendImage;

        if (_config.AiUseInterval)
        {
            IntervalRadio.IsChecked = true;
            IntervalPanel.Visibility = Visibility.Visible;
        }
        AiIntervalSlider.Value = _config.AiIntervalSeconds;
        IntervalLabel.Text = $"{_config.AiIntervalSeconds}s";

        // Shared
        QualitySlider.Value = _config.JpegQuality;
        QualityLabel.Text   = $"{_config.JpegQuality}%";
        UpdateRegionLabel();
        UpdateHotkeyLabel();
    }

    // ── Hotkeys ───────────────────────────────────────────────────────────

    private void RegisterHotkeys()
    {
        _hotkeys = new HotkeyService();
        _hotkeys.Attach(this);
        try
        {
            _hotkeys.Register(_config.HotkeySnapMod, _config.HotkeySnapKey, OnSnapshotHotkey);
        }
        catch { SetStatus("Warning: hotkey already in use by another app."); }
    }

    private void OnSnapshotHotkey()
    {
        Dispatcher.InvokeAsync(async () =>
        {
            if (_currentMode == AppMode.Discord)
                await SendDiscordSnapshotAsync();
            else if (_currentMode == AppMode.AiAnalysis && HotkeyRadio.IsChecked == true)
                await RunAiAnalysisAsync();
        });
    }

    private void UpdateHotkeyLabel()
    {
        SnapHotkeyDisplay.Text = HotkeyText(_config.HotkeySnapMod, _config.HotkeySnapKey);
        HotkeyLabel.Text = $"Hotkey: {SnapHotkeyDisplay.Text}";
    }

    private static string HotkeyText(uint mod, uint vk)
    {
        var parts = new List<string>();
        if ((mod & 0x0002) != 0) parts.Add("Ctrl");
        if ((mod & 0x0004) != 0) parts.Add("Shift");
        if ((mod & 0x0001) != 0) parts.Add("Alt");
        parts.Add(KeyInterop.KeyFromVirtualKey((int)vk).ToString());
        return string.Join("+", parts);
    }

    // ── Discord mode ──────────────────────────────────────────────────────

    private async Task SendDiscordSnapshotAsync()
    {
        string webhook = DiscordWebhookInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(webhook)) { SetStatus("Set a Discord webhook URL first."); return; }

        SetStatus("Capturing...");
        try
        {
            byte[] frame = CaptureService.Capture(_config.Region, _config.JpegQuality);
            bool sendImg = DiscordImageToggle.IsChecked == true;
            await DiscordService.PostAsync(webhook, "", sendImg ? frame : null);

            _snapCount++;
            DiscordLastSent.Text  = $"Last sent at {DateTime.Now:HH:mm:ss}";
            DiscordSentCount.Text = _snapCount.ToString();
            SetStatus($"Snapshot sent to Discord ✓");
        }
        catch (Exception ex) { SetStatus($"Error: {ex.Message}"); }
    }

    private async void TestDiscordWebhook_Click(object sender, RoutedEventArgs e)
    {
        string url = DiscordWebhookInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(url)) { SetStatus("Enter a webhook URL first."); return; }
        SetStatus("Testing webhook...");
        bool ok = await DiscordService.ValidateWebhookAsync(url);
        SetStatus(ok ? "Webhook valid ✓" : "Webhook invalid — check the URL.");
    }

    // ── AI mode ───────────────────────────────────────────────────────────

    private void AiProviderCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (AiProviderCombo.SelectedIndex < 0) return;
        var preset = Constants.AiPresets[AiProviderCombo.SelectedIndex];

        if (AiModelInput is not null) AiModelInput.Text = preset.DefaultModel;

        bool isCustom = preset.Name == "Custom";
        if (CustomEndpointPanel is not null)
            CustomEndpointPanel.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TriggerMode_Changed(object sender, RoutedEventArgs e)
    {
        if (IntervalPanel is null) return;
        bool useInterval = IntervalRadio?.IsChecked == true;
        IntervalPanel.Visibility = useInterval ? Visibility.Visible : Visibility.Collapsed;
        if (AiTestBtn is not null) AiTestBtn.Visibility = useInterval ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void AiTestNow_Click(object sender, RoutedEventArgs e)
        => await RunAiAnalysisAsync();

    private void StartInterval_Click(object sender, RoutedEventArgs e)
    {
        int secs = (int)AiIntervalSlider.Value;
        _intervalTimer = new System.Timers.Timer(secs * 1000);
        _intervalTimer.Elapsed += async (_, _) =>
        {
            await Dispatcher.InvokeAsync(async () => await RunAiAnalysisAsync());
        };
        _intervalTimer.AutoReset = true;
        _intervalTimer.Start();

        StartIntervalBtn.IsEnabled = false;
        StopIntervalBtn.IsEnabled  = true;
        SetStatus($"Interval running every {secs}s");
    }

    private void StopInterval_Click(object sender, RoutedEventArgs e)
    {
        _intervalTimer?.Stop();
        _intervalTimer?.Dispose();
        _intervalTimer = null;
        StartIntervalBtn.IsEnabled = true;
        StopIntervalBtn.IsEnabled  = false;
        SetStatus("Interval stopped.");
    }

    private async Task RunAiAnalysisAsync()
    {
        var preset = Constants.AiPresets[AiProviderCombo.SelectedIndex];

        string apiKey  = AiKeyInput.Password.Trim();
        string model   = AiModelInput.Text.Trim();
        string prompt  = AiPromptInput.Text.Trim();
        string webhook = AiWebhookInput.Text.Trim();
        string endpoint = preset.Name == "Custom"
            ? AiEndpointInput.Text.Trim()
            : preset.EndpointTemplate;

        if (string.IsNullOrWhiteSpace(apiKey))   { SetStatus("Enter an API key."); return; }
        if (string.IsNullOrWhiteSpace(webhook))  { SetStatus("Enter an output webhook URL."); return; }

        SetStatus("Capturing and analyzing...");
        try
        {
            byte[] frame   = CaptureService.Capture(_config.Region, _config.JpegQuality);
            string response = await AiService.AnalyzeAsync(endpoint, apiKey, model, preset.Format, prompt, frame);

            bool sendImg = AiImageToggle.IsChecked == true;
            string msg   = $"**AI Analysis** ({preset.Name} · {model})\n\n{response}";
            await DiscordService.PostAsync(webhook, msg, sendImg ? frame : null);

            // Truncate for display
            AiLastResponse.Text = response.Length > 300 ? response[..300] + "…" : response;
            SetStatus("Analysis sent to Discord ✓");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
        }
    }

    private async void TestAiWebhook_Click(object sender, RoutedEventArgs e)
    {
        string url = AiWebhookInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(url)) { SetStatus("Enter a webhook URL first."); return; }
        bool ok = await DiscordService.ValidateWebhookAsync(url);
        SetStatus(ok ? "Webhook valid ✓" : "Webhook invalid — check the URL.");
    }

    private void IntervalSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        string s = $"{(int)e.NewValue}s";
        if (IntervalLabel is not null)     IntervalLabel.Text     = s;
        if (IntervalCardLabel is not null) IntervalCardLabel.Text = s;
    }

    // ── Shared settings ───────────────────────────────────────────────────

    private void SelectRegion_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        var selector = new RegionSelectorWindow();
        if (selector.ShowDialog() == true && selector.SelectedRegion is not null)
        {
            var r = selector.SelectedRegion;
            _config.Region = new CaptureRegion { X = r.X, Y = r.Y, Width = r.Width, Height = r.Height };
            UpdateRegionLabel();
        }
        Show();
    }

    private void ResetRegion_Click(object sender, RoutedEventArgs e)
    {
        _config.Region = null;
        UpdateRegionLabel();
    }

    private void UpdateRegionLabel()
    {
        RegionLabel.Text = _config.Region is { } r
            ? $"{r.Width}×{r.Height} at ({r.X},{r.Y})"
            : "Full screen";
    }

    private void QualitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _config.JpegQuality = (int)e.NewValue;
        if (QualityLabel is not null) QualityLabel.Text = $"{_config.JpegQuality}%";
    }

    // ── Tray ─────────────────────────────────────────────────────────────

    private void SetupTray()
    {
        _tray = new System.Windows.Forms.NotifyIcon
        {
            Text    = Constants.AppName,
            Visible = true,
        };

        // Try load icon
        string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "tray.ico");
        if (System.IO.File.Exists(iconPath))
            _tray.Icon = new System.Drawing.Icon(iconPath);

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => { Show(); WindowState = WindowState.Normal; Activate(); });
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (_, _) => { SaveConfig(); _tray.Visible = false; Environment.Exit(0); });
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => { Show(); WindowState = WindowState.Normal; Activate(); };
    }

    // (MinimizeToTray handled by MinimizeBtn_Click / tray)

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void SignOut_Click(object sender, RoutedEventArgs e)
    {
        _config.IdToken = null;
        _config.RefreshToken = null;
        _config.Uid = null;
        SaveConfig();

        _hotkeys?.Dispose();
        _intervalTimer?.Stop();
        _tray!.Visible = false;

        new LoginWindow().Show();
        Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Minimize to tray instead of closing (user uses tray Exit to fully quit)
        e.Cancel = true;
        SaveConfig();
        Hide();
    }

    private void SaveConfig()
    {
        _config.DiscordWebhookUrl  = DiscordWebhookInput.Text.Trim();
        _config.DiscordSendImage   = DiscordImageToggle.IsChecked == true;
        _config.AiProviderName     = AiProviderCombo.SelectedItem?.ToString() ?? "Gemini";
        _config.AiApiKey           = AiKeyInput.Password;
        _config.AiModel            = AiModelInput.Text.Trim();
        _config.AiEndpointUrl      = AiEndpointInput.Text.Trim();
        _config.AiPrompt           = AiPromptInput.Text.Trim();
        _config.AiOutputWebhookUrl = AiWebhookInput.Text.Trim();
        _config.AiSendImage        = AiImageToggle.IsChecked == true;
        _config.AiUseInterval      = IntervalRadio.IsChecked == true;
        _config.AiIntervalSeconds  = (int)AiIntervalSlider.Value;
        _config.JpegQuality        = (int)QualitySlider.Value;
        _config.Mode               = _currentMode;

        // Sync format from selected provider
        int idx = AiProviderCombo.SelectedIndex;
        if (idx >= 0 && idx < Constants.AiPresets.Length)
            _config.AiFormat = Constants.AiPresets[idx].Format;

        ConfigService.Save(_config);
    }

    private void SetStatus(string msg)
        => Dispatcher.Invoke(() => StatusLabel.Text = msg);
}
