using FZScout.Site;

namespace FZScout;

/// <summary>Checkboxes apply as soon as they change. The API key is saved only when Verify finds the site accepts it.</summary>
class SettingsWindow : FixedSizeDialog
{
    const string GetApiKeyUrl = "https://fangbreaker.zone/account";
    const string ApiKeyHelpUrl = "https://jaypnw.github.io/FZScout/create-api-key.html";

    readonly LaunchAtLogin _launchAtLogin;
    readonly ShowToastNotifications _showToastNotifications;
    readonly IApiKeyStore _apiKeyStore;
    readonly FangbreakerZoneClient _fangbreakerZoneClient;
    readonly Log _log;
    readonly TextBox _apiKeyBox;
    readonly Button _verifyButton;
    readonly Label _apiKeyVerificationResult;
    string _savedApiKeyText;

    public SettingsWindow(LaunchAtLogin launchAtLogin, ShowToastNotifications showToastNotifications, IApiKeyStore apiKeyStore, FangbreakerZoneClient fangbreakerZoneClient, Log log)
    {
        this._launchAtLogin = launchAtLogin;
        this._showToastNotifications = showToastNotifications;
        this._apiKeyStore = apiKeyStore;
        this._fangbreakerZoneClient = fangbreakerZoneClient;
        this._log = log;
        Text = "FZScout Settings";

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Padding = new Padding(12) };
        this._savedApiKeyText = apiKeyStore.Load()?.Value ?? "";
        this._apiKeyBox = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true, Text = this._savedApiKeyText };
        this._verifyButton = new Button { Text = "Verify", AutoSize = true };
        this._verifyButton.Click += async (_, _) => await VerifyAndSaveApiKeyAsync();
        AcceptButton = this._verifyButton;
        layout.Controls.Add(ApiKeyRowWithTheBoxFillingTheSpaceBetween(this._apiKeyBox, this._verifyButton), 0, 0);
        var help = AddApiKeyHelpRow(layout);
        this._apiKeyVerificationResult = new Label { AutoSize = true, MaximumSize = new Size(help.PreferredSize.Width, 0), Visible = false };
        layout.Controls.Add(this._apiKeyVerificationResult, 0, 2);
        AddLaunchAtLoginRow(layout);
        AddShowToastNotificationsRow(layout);
        AddCloseButtonRow(layout);
        this._apiKeyBox.TextChanged += (_, _) => OnApiKeyBoxTextChanged();
        AllowVerifyingOnlyANewApiKey();
        layout.MinimumSize = new Size(layout.PreferredSize.Width * 13 / 10, 0);
        Controls.Add(layout);
    }

    /// <summary>Raised only after the site accepted the key.</summary>
    public event Action? ApiKeySaved;

    string ApiKeyBoxText => this._apiKeyBox.Text.Trim();

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        this._apiKeyBox.Focus();
    }

    void OnApiKeyBoxTextChanged()
    {
        this._apiKeyVerificationResult.Visible = false;
        AllowVerifyingOnlyANewApiKey();
    }

    void AllowVerifyingOnlyANewApiKey() => this._verifyButton.Enabled = ApiKeyBoxText.Length > 0 && ApiKeyBoxText != this._savedApiKeyText;

    /// <summary>Stretches across the window, so the box grows to push Verify to the right edge.</summary>
    static TableLayoutPanel ApiKeyRowWithTheBoxFillingTheSpaceBetween(TextBox apiKeyBox, Button verifyButton)
    {
        var apiKeyLabel = new Label { Text = "API key:", Anchor = AnchorStyles.Left, AutoSize = true };
        var row = new TableLayoutPanel { ColumnCount = 3, RowCount = 1, AutoSize = true, Dock = DockStyle.Fill, Margin = Padding.Empty };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.Controls.Add(apiKeyLabel, 0, 0);
        row.Controls.Add(apiKeyBox, 1, 0);
        row.Controls.Add(verifyButton, 2, 0);
        return row;
    }

    /// <returns>The help line, whose width sets how wide the window is.</returns>
    static LinkLabel AddApiKeyHelpRow(TableLayoutPanel layout)
    {
        var helpText = "Get an API key from fangbreaker.zone/account. Need help?";
        var help = new LinkLabel { Text = helpText, AutoSize = true };
        help.Links.Add(helpText.IndexOf("fangbreaker.zone/account", StringComparison.Ordinal), "fangbreaker.zone/account".Length, GetApiKeyUrl);
        help.Links.Add(helpText.IndexOf("Need help?", StringComparison.Ordinal), "Need help?".Length, ApiKeyHelpUrl);
        help.LinkClicked += (_, e) => Browser.Open((string)e.Link!.LinkData!);
        layout.Controls.Add(help, 0, 1);
        return help;
    }

    void AddLaunchAtLoginRow(TableLayoutPanel layout)
    {
        var launchAtLoginBox = new CheckBox { Text = "Launch FZScout when I log in to Windows", AutoSize = true, Checked = this._launchAtLogin.IsOn };
        launchAtLoginBox.CheckedChanged += (_, _) => ApplyLaunchAtLogin(launchAtLoginBox.Checked);
        layout.Controls.Add(launchAtLoginBox, 0, 3);
    }

    void AddShowToastNotificationsRow(TableLayoutPanel layout)
    {
        var showToastNotificationsBox = new CheckBox { Text = "Show toast notifications", AutoSize = true, Checked = this._showToastNotifications.IsOn };
        showToastNotificationsBox.CheckedChanged += (_, _) => ApplyShowToastNotifications(showToastNotificationsBox.Checked);
        layout.Controls.Add(showToastNotificationsBox, 0, 4);
    }

    void AddCloseButtonRow(TableLayoutPanel layout)
    {
        var closeButton = new Button { Text = "Close", AutoSize = true };
        closeButton.Click += (_, _) => Close();
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
        buttons.Controls.Add(closeButton);
        CancelButton = closeButton;
        layout.Controls.Add(buttons, 0, 5);
    }

    async Task VerifyAndSaveApiKeyAsync()
    {
        var apiKey = new ApiKey(ApiKeyBoxText);
        this._apiKeyBox.Enabled = false;
        this._verifyButton.Enabled = false;
        this._apiKeyVerificationResult.Visible = false;
        var problem = await CheckAndSaveApiKeyAsync(apiKey);
        if (IsDisposed)
        {
            return;
        }

        if (problem is not null)
        {
            ShowApiKeyVerificationResult(problem, Color.Firebrick);
            return;
        }

        ShowApiKeyVerificationResult("API key verified and saved.", Color.ForestGreen);
    }

    /// <returns>What went wrong, or null if the key was saved.</returns>
    async Task<string?> CheckAndSaveApiKeyAsync(ApiKey apiKey)
    {
        var result = await this._fangbreakerZoneClient.CheckApiKeyAsync(apiKey);
        if (result.Outcome == ApiOutcome.KeyRejected)
        {
            return "The site rejected this API key.";
        }

        if (!result.IsSuccess)
        {
            return $"Couldn't check the API key. {result.Error}";
        }

        try
        {
            this._apiKeyStore.Save(apiKey);
        }
        catch (Exception exception)
        {
            return $"Couldn't save the API key: {exception.Message}";
        }

        this._savedApiKeyText = apiKey.Value;
        this._log.Info($"API key verified and saved to {this._apiKeyStore.StorageLocationForDisplay}.");
        ApiKeySaved?.Invoke();
        return null;
    }

    void ShowApiKeyVerificationResult(string message, Color color)
    {
        this._apiKeyBox.Enabled = true;
        AllowVerifyingOnlyANewApiKey();
        this._apiKeyVerificationResult.Text = message;
        this._apiKeyVerificationResult.ForeColor = color;
        this._apiKeyVerificationResult.Visible = true;
        this._apiKeyBox.Focus();
    }

    void ApplyLaunchAtLogin(bool wantsLaunchAtLogin)
    {
        if (wantsLaunchAtLogin == this._launchAtLogin.IsOn)
        {
            return;
        }

        if (wantsLaunchAtLogin)
        {
            this._launchAtLogin.TurnOn(Environment.ProcessPath!);
            return;
        }

        this._launchAtLogin.TurnOff();
    }

    void ApplyShowToastNotifications(bool wantsToastNotifications)
    {
        if (wantsToastNotifications)
        {
            this._showToastNotifications.TurnOn();
            return;
        }

        this._showToastNotifications.TurnOff();
    }
}
