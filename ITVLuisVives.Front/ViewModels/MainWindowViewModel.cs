using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ITVLuisVives.Front.ViewModels;

/// <summary>
///     ViewModel principal de la aplicación.
///     Maneja la navegación interna y las acciones de los menús usando CommunityToolkit.Mvvm.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    // ====================================================================
    // PROPIEDADES OBSERVABLES
    // ====================================================================

    [ObservableProperty] 
    private object _currentView;

    [ObservableProperty] 
    private string _tipoBorradoStatus = "Borrado: Lógico (Flag)";

    // ====================================================================
    // INICIALIZACIÓN / CONSTRUCTOR
    // ====================================================================
    public MainWindowViewModel()
    {
        _currentView = new CitasViewModel();
    }

    // ====================================================================
    // COMANDOS DE NAVEGACIÓN
    // ====================================================================

    [RelayCommand]
    private void NavegarCitas()
    {
        CurrentView = new CitasViewModel();
    }

    [RelayCommand]
    private void NavegarAcercaDe()
    {
        var acercaDeWin = new Views.AcercaDeWindow();
    
        acercaDeWin.Owner = Application.Current.MainWindow;
    
        acercaDeWin.ShowDialog();
    }

    // ====================================================================
    // COMANDOS DEL MENÚ SUPERIOR
    // ====================================================================

    [RelayCommand]
    private void ExportarDatos()
    {
        MessageBox.Show("Exportando datos a JSON...", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void ImportarDatos()
    {
        MessageBox.Show("Importando datos desde JSON...", "Importar", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void Salir()
    {
        var resultado = MessageBox.Show("¿Estás seguro de que quieres salir?", "Confirmar salida", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (resultado == MessageBoxResult.Yes)
        {
            Application.Current.Shutdown();
        }
    }
}