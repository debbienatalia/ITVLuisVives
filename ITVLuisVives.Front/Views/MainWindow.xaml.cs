using System.Windows;
using ITVLuisVives.Front.ViewModels;

namespace ITVLuisVives.Front.Views;

/// <summary>
/// Lógica interactiva para MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        this.DataContext = new MainWindowViewModel();
    }
}