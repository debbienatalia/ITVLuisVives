using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;

namespace ITVLuisVives.Front.ViewModels;

public partial class AcercaDeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _autor = "Debbie Natalia Ante Ramírez";

    [ObservableProperty]
    private string _version = "1.0.0-Stable";

    [ObservableProperty]
    private string _proyecto = "ITV Luis Vives — Sistema de Gestión de Citas";

    [ObservableProperty]
    private string _curso = "Desarrollo de Aplicaciones Web (DAW) — 2026";

    [ObservableProperty]
    private string _urlRepositorio = "https://github.com/debbienatalia/ITVLuisVives";

    /// <summary>
    ///     Comando que abre el navegador web con tu repositorio de GitHub.
    /// </summary>
    [RelayCommand]
    private void AbrirRepositorio()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = UrlRepositorio,
                UseShellExecute = true
            });
        }
        catch (Exception)
        {
        }
    }
}