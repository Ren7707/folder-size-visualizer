using System.Text;
using System.Windows;
using System.ComponentModel;
using FolderSizeVisualizer.App.ViewModels;
using FolderSizeVisualizer.Core.Models;

namespace FolderSizeVisualizer.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainViewModel();
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        DataContext = viewModel;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainViewModel viewModel) return;
        if (e.PropertyName == nameof(MainViewModel.SelectedTheme)) ApplyTheme(viewModel.SelectedTheme);
        if (e.PropertyName == nameof(MainViewModel.SelectedLanguage)) ApplyLanguage(viewModel.SelectedLanguage);
    }

    private static void ApplyTheme(ThemeMode theme)
    {
        ReplaceDictionary(theme == ThemeMode.Dark ? "Resources/Themes.Dark.xaml" : "Resources/Themes.Light.xaml", "Themes.");
    }

    private static void ApplyLanguage(AppLanguage language)
    {
        ReplaceDictionary(language == AppLanguage.English ? "Resources/Strings.en-US.xaml" : "Resources/Strings.zh-CN.xaml", "Strings.");
    }

    private static void ReplaceDictionary(string source, string marker)
    {
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var existing = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.Contains(marker) == true);
        if (existing is not null) dictionaries.Remove(existing);
        dictionaries.Add(new ResourceDictionary { Source = new Uri(source, UriKind.Relative) });
    }
}
