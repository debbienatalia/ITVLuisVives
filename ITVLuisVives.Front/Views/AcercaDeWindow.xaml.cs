using System.Windows;
using ITVLuisVives.Front.ViewModels;

namespace ITVLuisVives.Front.Views;

/// <summary>
/// Lógica de interacción para AcercaDeWindow.xaml
/// </summary>
public partial class AcercaDeWindow : Window
{
    public AcercaDeWindow()
    {
        InitializeComponent();
        
        DataContext = new AcercaDeViewModel();
    }

    /// <summary>
    ///     Cierra la ventana flotante de manera segura al hacer clic.
    /// </summary>
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}