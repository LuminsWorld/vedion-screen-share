using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using VedionScreenShare.Services.Auth;

namespace VedionScreenShare.Windows;

public partial class LoginWindow : Window
{
    private bool _isSignUp = false;

    public FirebaseUser? LoggedInUser { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => EmailInput.Focus();
    }

    private void DragBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove();
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

    // ── Toggle sign-in / sign-up ──────────────────────────────────────────

    private void ToggleLink_Click(object sender, MouseButtonEventArgs e)
    {
        _isSignUp = !_isSignUp;
        if (_isSignUp)
        {
            TitleLabel.Text    = "Create account";
            SubLabel.Text      = "Already have one? Sign in above.";
            PrimaryButton.Content = "CREATE ACCOUNT";
            NamePanel.Visibility  = Visibility.Visible;
            ForgotLink.Visibility = Visibility.Collapsed;
            TogglePrompt.Text  = "Already have an account?";
            ToggleLink.Text    = "Sign in";
        }
        else
        {
            TitleLabel.Text    = "Sign in";
            SubLabel.Text      = "Access your Vedion Screen Share license.";
            PrimaryButton.Content = "SIGN IN";
            NamePanel.Visibility  = Visibility.Collapsed;
            ForgotLink.Visibility = Visibility.Visible;
            TogglePrompt.Text  = "Don't have an account?";
            ToggleLink.Text    = "Create one";
        }
        StatusLabel.Text = "";
    }

    // ── Actions ───────────────────────────────────────────────────────────

    private async void PrimaryButton_Click(object sender, RoutedEventArgs e)
        => await ExecuteAsync(_isSignUp ? DoSignUp : DoSignIn);

    private async void GoogleButton_Click(object sender, RoutedEventArgs e)
        => await ExecuteAsync(DoGoogleSignIn);

    private async void ForgotLink_Click(object sender, MouseButtonEventArgs e)
    {
        string email = EmailInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(email)) { ShowError("Enter your email first."); return; }
        try
        {
            await FirebaseAuthService.SendPasswordResetAsync(email);
            ShowStatus("Password reset email sent.", "#00FF41");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return) PrimaryButton_Click(sender, new RoutedEventArgs());
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
        e.Handled = true;
    }

    // ── Auth actions ──────────────────────────────────────────────────────

    private async Task DoSignIn()
    {
        var user = await FirebaseAuthService.SignInAsync(EmailInput.Text.Trim(), PasswordInput.Password);
        Succeed(user);
    }

    private async Task DoSignUp()
    {
        if (PasswordInput.Password.Length < 6) throw new Exception("Password must be at least 6 characters.");
        var user = await FirebaseAuthService.SignUpAsync(EmailInput.Text.Trim(), PasswordInput.Password, NameInput.Text.Trim());
        Succeed(user);
    }

    private async Task DoGoogleSignIn()
    {
        ShowStatus("Opening browser for Google sign-in…", "#888");
        var user = await FirebaseAuthService.SignInWithGoogleAsync();
        Succeed(user);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task ExecuteAsync(Func<Task> action)
    {
        SetBusy(true);
        StatusLabel.Text = "";
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Succeed(FirebaseUser user)
    {
        LoggedInUser = user;
        DialogResult = true;
        Close();
    }

    private void SetBusy(bool busy)
    {
        PrimaryButton.IsEnabled = !busy;
        GoogleButton.IsEnabled  = !busy;
        PrimaryButton.Content   = busy ? "..." : (_isSignUp ? "CREATE ACCOUNT" : "SIGN IN");
    }

    private void ShowError(string msg)
    {
        StatusBorder.Visibility = Visibility.Visible;
        StatusLabel.Text        = msg;
        StatusLabel.Foreground  = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0xf8, 0x71, 0x71));
    }

    private void ShowStatus(string msg, string color)
    {
        StatusBorder.Visibility   = Visibility.Visible;
        StatusLabel.Text          = msg;
        StatusLabel.Foreground    = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
        StatusBorder.Background   = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x1a, 0x00, 0x20, 0x00));
        StatusBorder.BorderBrush  = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x44, 0x00, 0xFF, 0x41));
    }
}
