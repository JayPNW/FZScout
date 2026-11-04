using FZScout.Site;

namespace FZScout;

class SettingsWindow : Form
{
    const string GetApiKeyUrl = "https://fangbreaker.zone/account";
    const string ApiKeyHelpUrl = "https://jaypnw.github.io/FZScout/create-api-key.html";

    readonly TextBox _apiKeyBox;
    bool _wasSaved;

    public SettingsWindow()
    {
        Text = "FZScout Settings";
        Icon = SystemIcons.Application;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        StartPosition = FormStartPosition.Manual;

        var apiKeyLabel = new Label { Text = "API key:", Anchor = AnchorStyles.Left, AutoSize = true };
        this._apiKeyBox = new TextBox { Width = 400, UseSystemPasswordChar = true };

        var helpText = "Get an API key from fangbreaker.zone/account. Need help?";
        var help = new LinkLabel { Text = helpText, AutoSize = true };
        help.Links.Add(helpText.IndexOf("fangbreaker.zone/account", StringComparison.Ordinal), "fangbreaker.zone/account".Length, GetApiKeyUrl);
        help.Links.Add(helpText.IndexOf("Need help?", StringComparison.Ordinal), "Need help?".Length, ApiKeyHelpUrl);
        help.LinkClicked += (_, e) => Browser.Open((string)e.Link!.LinkData!);

        var saveButton = new Button { Text = "Save", AutoSize = true };
        saveButton.Click += (_, _) => Save();
        var cancelButton = new Button { Text = "Cancel", AutoSize = true };
        cancelButton.Click += (_, _) => Close();
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(saveButton);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Padding = new Padding(12) };
        layout.Controls.Add(apiKeyLabel, 0, 0);
        layout.Controls.Add(this._apiKeyBox, 1, 0);
        layout.Controls.Add(help, 1, 1);
        layout.Controls.Add(buttons, 0, 2);
        layout.SetColumnSpan(buttons, 2);
        Controls.Add(layout);
    }

    /// <summary>Null if the user cancelled or left the box empty.</summary>
    public ApiKey? EnteredApiKey
    {
        get
        {
            var text = this._apiKeyBox.Text.Trim();
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
        this._apiKeyBox.Focus();
    }

    void Save()
    {
        this._wasSaved = true;
        Close();
    }
}
