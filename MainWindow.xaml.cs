using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Nioh3AffixEditor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly System.Net.Http.HttpClient _httpClient = new();
    private readonly Nioh3AffixEditor.ViewModels.MainViewModel _viewModel;
    private readonly Nioh3AffixEditor.Engine.NativeAffixEngine _affixEngine;

    public MainWindow()
    {
        InitializeComponent();

        var cacheStore = new Nioh3AffixEditor.Services.UpdateCacheStore(Nioh3AffixEditor.Services.AppPaths.GetUpdateCachePath());
        var updateCheckService = new Nioh3AffixEditor.Services.UpdateCheckService(_httpClient, cacheStore);
        _affixEngine = new Nioh3AffixEditor.Engine.NativeAffixEngine();
        var processDiscovery = new Nioh3AffixEditor.Services.ProcessDiscoveryService();

        _viewModel = new Nioh3AffixEditor.ViewModels.MainViewModel(updateCheckService, _affixEngine, processDiscovery);
        DataContext = _viewModel;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _affixEngine.Dispose();
        _httpClient.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.InitializeAsync();
    }

    private static bool IsDigitsOnly(string text)
        => text.All(char.IsDigit);

    private static bool IsAffixIdAllowed(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        // Allow decimal digits and the special empty marker FFFFFFFF (typed as hex digits).
        // Also allow '-' for legacy -1 input (final validation occurs in ViewModel).
        foreach (char c in text)
        {
            if (char.IsDigit(c) || c is '-' or 'F' or 'f')
            {
                continue;
            }
            return false;
        }

        return true;
    }

    private void AffixIdTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsAffixIdAllowed(e.Text);
    }

    private void AffixIdTextBox_OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.SourceDataObject.GetDataPresent(DataFormats.Text, true))
        {
            e.CancelCommand();
            return;
        }

        var text = e.SourceDataObject.GetData(DataFormats.Text) as string ?? "";
        if (!IsAffixIdAllowed(text))
        {
            e.CancelCommand();
        }
    }

    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsDigitsOnly(e.Text);
    }

    private void NumericTextBox_OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.SourceDataObject.GetDataPresent(DataFormats.Text, true))
        {
            e.CancelCommand();
            return;
        }

        var text = e.SourceDataObject.GetData(DataFormats.Text) as string ?? "";
        if (!IsDigitsOnly(text))
        {
            e.CancelCommand();
        }
    }
}
