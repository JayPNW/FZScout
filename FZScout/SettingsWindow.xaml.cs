using System.Windows;
using System.Windows.Navigation;
using FZScout.ViewModels;

namespace FZScout;

/// <summary>Checkboxes apply as soon as they change. The API key is saved only when Verify finds the site accepts it.</summary>
partial class SettingsWindow : FixedSizeDialog
{
    readonly AppViewModel _viewModel;

    /// <summary>A PasswordBox can't be bound, so its text is copied to <see cref="AppViewModel.ApiKey"/> by hand.</summary>
    public SettingsWindow(AppViewModel viewModel)
    {
        InitializeComponent();
        this._viewModel = viewModel;
        DataContext = viewModel;
        this.ApiKeyBox.Password = viewModel.ApiKey;
        Loaded += (_, _) => this.ApiKeyBox.Focus();
        this.ApiKeyBox.IsEnabledChanged += RefocusApiKeyBoxWhenVerificationFinishes;
    }

    /// <summary>The box is disabled while verifying, which takes away its keyboard focus.</summary>
    void RefocusApiKeyBoxWhenVerificationFinishes(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            this.ApiKeyBox.Focus();
        }
    }

    void CopyApiKeyToViewModel(object sender, RoutedEventArgs e) => this._viewModel.ApiKey = this.ApiKeyBox.Password;

    void OpenLinkInBrowser(object sender, RequestNavigateEventArgs e) => Browser.Open(e.Uri.AbsoluteUri);

    void CloseWindow(object sender, RoutedEventArgs e) => Close();
}
