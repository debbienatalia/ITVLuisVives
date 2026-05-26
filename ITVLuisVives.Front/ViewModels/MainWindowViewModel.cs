using System;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ITVLuisVives.Back.Services;
using ITVLuisVives.Back.Models;

namespace ITVLuisVives.Front.ViewModels;

/// <summary>
///     ViewModel principal de la aplicación.
///     Maneja la navegación interna y las acciones de los menús usando CommunityToolkit.Mvvm y DI.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private const string FiltroMaestro = "Todos los formatos (*.json, *.xml, *.csv)|*.json;*.xml;*.csv|JSON (*.json)|*.json|XML (*.xml)|*.xml|CSV (*.csv)|*.csv";

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
        _currentView = App.ServiceProvider.GetRequiredService<CitasViewModel>();
    }

    // ====================================================================
    // COMANDOS DE NAVEGACIÓN
    // ====================================================================

    [RelayCommand]
    private void NavegarCitas()
    {
        CurrentView = App.ServiceProvider.GetRequiredService<CitasViewModel>();
    }
    
    [RelayCommand]
    private void NavegarImportExport()
    {
        CurrentView = App.ServiceProvider.GetRequiredService<ImportExportViewModel>();
    }

    [RelayCommand]
    private void NavegarInformes()
    {
        CurrentView = App.ServiceProvider.GetRequiredService<InformesViewModel>();
    }

    [RelayCommand]
    private void NavegarAcercaDe()
    {
        var acercaDeWin = App.ServiceProvider.GetRequiredService<Views.AcercaDeWindow>();
    
        acercaDeWin.Owner = Application.Current.MainWindow;
    
        acercaDeWin.ShowDialog();
    }

    // ====================================================================
    // COMANDOS DEL MENÚ SUPERIOR
    // ====================================================================

    [RelayCommand]
    private void ExportarDatos()
    {
        var citaService = App.ServiceProvider.GetRequiredService<ICitaService>();
        var importExportService = App.ServiceProvider.GetRequiredService<IImportExportService>();

        var todasLasCitas = citaService.ObtenerFiltradas(
            dni: null, matricula: null, estado: null, fechaDesde: null, fechaHasta: null,
            pagina: 1, tamanoPagina: int.MaxValue, incluirEliminados: true
        ).ToList();

        var sfd = new SaveFileDialog 
        { 
            Filter = FiltroMaestro, 
            FileName = $"Backup_Global_ITV_{DateTime.Now:yyyyMMdd}" 
        };

        if (sfd.ShowDialog() == true)
        {
            var resultado = importExportService.ExportarDatos(todasLasCitas, sfd.FileName);

            if (resultado.IsSuccess)
            {
                var extensionGrafica = Path.GetExtension(sfd.FileName).ToUpper().TrimStart('.');
                MessageBox.Show(
                    $"Copia de seguridad global realizada con éxito.\nSe exportaron {resultado.Value} registros en formato [.{extensionGrafica}].", 
                    "Exportación Exitosa", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information
                );
            }
            else
            {
                MessageBox.Show(
                    $"Error al procesar la exportación del sistema: {resultado.Error.Message}", 
                    "Error de Persistencia", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error
                );
            }
        }
    }

    [RelayCommand]
    private void ImportarDatos()
    {
        var citaService = App.ServiceProvider.GetRequiredService<ICitaService>();
        var importExportService = App.ServiceProvider.GetRequiredService<IImportExportService>();

        var ofd = new OpenFileDialog { Filter = FiltroMaestro };

        if (ofd.ShowDialog() == true)
        {
            var resultado = importExportService.ImportarDatos(ofd.FileName);

            if (resultado.IsSuccess)
            {
                var citasImportadas = resultado.Value.ToList();
                int exitos = 0;
                int fallos = 0;

                foreach (var cita in citasImportadas)
                {
                    var resAgendar = citaService.Agendar(cita);
                    if (resAgendar.IsSuccess)
                        exitos++;
                    else
                        fallos++;
                }

                MessageBox.Show(
                    $"Proceso de restauración completado.\n\n· Registros insertados con éxito: {exitos}\n· Registros omitidos por reglas de negocio: {fallos}", 
                    "Importación Finalizada", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information
                );
            }
            else
            {
                MessageBox.Show(
                    $"No se pudo procesar la lectura del archivo: {resultado.Error.Message}", 
                    "Error de Formato", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error
                );
            }
        }
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