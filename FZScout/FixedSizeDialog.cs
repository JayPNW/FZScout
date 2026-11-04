namespace FZScout;

/// <summary>A dialog the user can't resize, which opens over another window without blocking it.</summary>
class FixedSizeDialog : Form
{
    public FixedSizeDialog()
    {
        Icon = AppIcon.Load();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        StartPosition = FormStartPosition.Manual;
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
    }
}
