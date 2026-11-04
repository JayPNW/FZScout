using FZScout.Site;

namespace FZScout;

/// <summary>Saves only an API key the site accepts. Stays open showing what went wrong otherwise.</summary>
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
    readonly Label _apiKeyProblem;
    readonly CheckBox _launchAtLoginBox;
    readonly CheckBox _showToastNotificationsBox;
    readonly Button _saveButton;

    public SettingsWindow(LaunchAtLogin launchAtLogin, ShowToastNotifications showToastNotifications, IApiKeyStore apiKeyStore, FangbreakerZoneClient fangbreakerZoneClient, Log log)
    {
        this._launchAtLogin = launchAtLogin;
        this._showToastNotifications = showToastNotifications;
        this._apiKeyStore = apiKeyStore;
        this._fangbreakerZoneClient = fangbreakerZoneClient;
        this._log = log;
        Text = "FZScout Settings";

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Padding = new Padding(12) };
        this._apiKeyBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, UseSystemPasswordChar = true, Text = apiKeyStore.Load()?.Value ?? "" };
        var help = AddApiKeyRows(layout, this._apiKeyBox);
        this._apiKeyProblem = new Label { AutoSize = true, MaximumSize = new Size(help.PreferredSize.Width, 0), ForeColor = Color.Firebrick, Visible = false };
        layout.Controls.Add(this._apiKeyProblem, 1, 2);
        this._launchAtLoginBox = AddLaunchAtLoginRow(layout, launchAtLogin);
        this._showToastNotificationsBox = AddShowToastNotificationsRow(layout, showToastNotifications);
        this._saveButton = AddSaveAndCancelButtonsRow(layout);
        this._apiKeyBox.TextChanged += (_, _) => AllowSavingOnlyWithAnApiKey();
        AllowSavingOnlyWithAnApiKey();
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

    void AllowSavingOnlyWithAnApiKey() => this._saveButton.Enabled = ApiKeyBoxText.Length > 0;

    /// <returns>The help line, whose width sets how wide the API key column is.</returns>
    static LinkLabel AddApiKeyRows(TableLayoutPanel layout, TextBox apiKeyBox)
    {
        var apiKeyLabel = new Label { Text = "API key:", Anchor = AnchorStyles.Left, AutoSize = true };

        var helpText = "Get an API key from fangbreaker.zone/account. Need help?";
        var help = new LinkLabel { Text = helpText, AutoSize = true };
        help.Links.Add(helpText.IndexOf("fangbreaker.zone/account", StringComparison.Ordinal), "fangbreaker.zone/account".Length, GetApiKeyUrl);
        help.Links.Add(helpText.IndexOf("Need help?", StringComparison.Ordinal), "Need help?".Length, ApiKeyHelpUrl);
        help.LinkClicked += (_, e) => Browser.Open((string)e.Link!.LinkData!);

        layout.Controls.Add(apiKeyLabel, 0, 0);
        layout.Controls.Add(apiKeyBox, 1, 0);
        layout.Controls.Add(help, 1, 1);
        return help;
    }

    static CheckBox AddLaunchAtLoginRow(TableLayoutPanel layout, LaunchAtLogin launchAtLogin)
    {
        var launchAtLoginBox = new CheckBox { Text = "Launch FZScout when I log in to Windows", AutoSize = true, Checked = launchAtLogin.IsOn };
        layout.Controls.Add(launchAtLoginBox, 0, 3);
        layout.SetColumnSpan(launchAtLoginBox, 2);
        return launchAtLoginBox;
    }

    static CheckBox AddShowToastNotificationsRow(TableLayoutPanel layout, ShowToastNotifications showToastNotifications)
    {
        var showToastNotificationsBox = new CheckBox { Text = "Show toast notifications", AutoSize = true, Checked = showToastNotifications.IsOn };
        layout.Controls.Add(showToastNotificationsBox, 0, 4);
        layout.SetColumnSpan(showToastNotificationsBox, 2);
        return showToastNotificationsBox;
    }

    Button AddSaveAndCancelButtonsRow(TableLayoutPanel layout)
    {
        var saveButton = new Button { Text = "Save", AutoSize = true };
        saveButton.Click += async (_, _) => await SaveIfApiKeyIsAcceptedAsync();
        var cancelButton = new Button { Text = "Cancel", AutoSize = true };
        cancelButton.Click += (_, _) => Close();
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(saveButton);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        layout.Controls.Add(buttons, 0, 5);
        layout.SetColumnSpan(buttons, 2);
        return saveButton;
    }

    async Task SaveIfApiKeyIsAcceptedAsync()
    {
        var apiKey = new ApiKey(ApiKeyBoxText);
        this._apiKeyBox.Enabled = false;
        this._saveButton.Enabled = false;
        this._apiKeyProblem.Visible = false;
        var problem = await CheckAndSaveApiKeyAsync(apiKey);
        if (IsDisposed)
        {
            return;
        }

        if (problem is not null)
        {
            ShowApiKeyProblem(problem);
            return;
        }

        SaveLaunchAtLogin();
        SaveShowToastNotifications();
        Close();
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

        this._log.Info($"API key verified and saved to {this._apiKeyStore.StorageLocationForDisplay}.");
        ApiKeySaved?.Invoke();
        return null;
    }

    void ShowApiKeyProblem(string problem)
    {
        this._apiKeyProblem.Text = problem;
        this._apiKeyProblem.Visible = true;
        this._apiKeyBox.Enabled = true;
        AllowSavingOnlyWithAnApiKey();
        this._apiKeyBox.Focus();
    }

    void SaveLaunchAtLogin()
    {
        var wantsLaunchAtLogin = this._launchAtLoginBox.Checked;
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

    void SaveShowToastNotifications()
    {
        if (this._showToastNotificationsBox.Checked)
        {
            this._showToastNotifications.TurnOn();
            return;
        }

        this._showToastNotifications.TurnOff();
    }
}
