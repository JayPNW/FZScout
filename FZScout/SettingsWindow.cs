using FZScout.Site;

namespace FZScout;

class SettingsWindow : Form
{
    const string GetApiKeyUrl = "https://fangbreaker.zone/account";
    const string ApiKeyHelpUrl = "https://jaypnw.github.io/FZScout/create-api-key.html";

    readonly LaunchAtLogin _launchAtLogin;
    readonly TextBox? _apiKeyBox;
    readonly CheckBox _launchAtLoginBox;
    bool _wasSaved;

    /// <param name="asksForApiKey">Only when the app is waiting for one, since a running app keeps using the key it started with.</param>
    public SettingsWindow(LaunchAtLogin launchAtLogin, bool asksForApiKey)
    {
        this._launchAtLogin = launchAtLogin;
        Text = "FZScout Settings";
        Icon = AppIcon.Load();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        StartPosition = FormStartPosition.Manual;

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Padding = new Padding(12) };
        if (asksForApiKey)
        {
            this._apiKeyBox = new TextBox { Width = 400, UseSystemPasswordChar = true };
            AddApiKeyRows(layout, this._apiKeyBox);
        }

        this._launchAtLoginBox = new CheckBox { Text = "Launch FZScout when I log in to Windows", AutoSize = true, Checked = launchAtLogin.IsOn };
        layout.Controls.Add(this._launchAtLoginBox, 0, 2);
        layout.SetColumnSpan(this._launchAtLoginBox, 2);

        var saveButton = new Button { Text = "Save", AutoSize = true };
        saveButton.Click += (_, _) => Save();
        var cancelButton = new Button { Text = "Cancel", AutoSize = true };
        cancelButton.Click += (_, _) => Close();
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(saveButton);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        layout.Controls.Add(buttons, 0, 3);
        layout.SetColumnSpan(buttons, 2);
        Controls.Add(layout);
    }

    /// <summary>Null if the user cancelled, left the box empty, or wasn't asked for one.</summary>
    public ApiKey? EnteredApiKey
    {
        get
        {
            var text = this._apiKeyBox?.Text.Trim() ?? "";
            if (!this._wasSaved || text.Length == 0)
            {
                return null;
            }

            return new ApiKey(text);
        }
    }

    /// <summary>Shows the window over <paramref name="owner"/> without blocking it, so the log stays readable and scrollable.</summary>
    public void ShowCenteredOver(Form owner)
    {
        Owner = owner;
        CreateControl();
        Location = new Point(
            owner.Left + ((owner.Width - Width) / 2),
            owner.Top + ((owner.Height - Height) / 2));
        Show();
        Activate();
        this._apiKeyBox?.Focus();
    }

    static void AddApiKeyRows(TableLayoutPanel layout, TextBox apiKeyBox)
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
    }

    void Save()
    {
        this._wasSaved = true;
        SaveLaunchAtLogin();
        Close();
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
}
