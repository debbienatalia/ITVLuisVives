using System.Windows;
using ITVLuisVives.Front.ViewModels;
using ITVLuisVives.Front.Views;

namespace ITVLuisVives.Front;

/// <summary>
///     Punto de entrada de la aplicación WPF.
///     Configura el ciclo de vida inicial de la interfaz y la vinculación del patrón MVVM.
/// </summary>
public partial class App : Application
{
    /// <summary>
    ///     Método ejecutado al iniciar la aplicación.
    ///     Instancia la ventana principal, inicializa su contexto de datos y la despliega.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainWindow mainWindow = new MainWindow();

        MainWindowViewModel viewModel = new MainWindowViewModel();
        mainWindow.DataContext = viewModel;

        mainWindow.Show();
    }
}