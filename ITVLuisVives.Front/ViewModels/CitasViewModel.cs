using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Enums;

namespace ITVLuisVives.Front.ViewModels;

public partial class CitasViewModel : ObservableValidator
{
    [ObservableProperty]
    private ObservableCollection<Cita> _citas = new();

    // ─────────────────────────────────────────────────────────
    // PROPIEDADES DEL FORMULARIO CON VALIDACIÓN EN VIVO
    // ─────────────────────────────────────────────────────────
    
    private string _dni = string.Empty;
    [Required(ErrorMessage = "El DNI es totalmente obligatorio.")]
    [RegularExpression(@"^[0-9]{8}[A-Z]$", ErrorMessage = "Formato incorrecto (Ej: 12345678Z).")]
    public string Dni
    {
        get => _dni;
        set => SetProperty(ref _dni, value, true);
    }

    private string _matricula = string.Empty;
    [Required(ErrorMessage = "La matrícula no puede estar vacía.")]
    [RegularExpression(@"^\d{4}[A-Z]{3}$", ErrorMessage = "Formato español válido: 1234XYZ.")]
    public string Matricula
    {
        get => _matricula;
        set => SetProperty(ref _matricula, value, true);
    }

    [ObservableProperty]
    private DateTime _fechaInspeccion = DateTime.Today;

    [ObservableProperty]
    private string _observaciones = string.Empty;

    public CitasViewModel()
    {
        CargarDatosDemostracion();
    }

    private void CargarDatosDemostracion()
    {
        _citas.Add(new Cita { Dni = "12345678Z", VehiculoMatricula = "4321KFC", FechaInspeccion = DateTime.Now.AddHours(1), Estado = EstadoCita.Pendiente, Observaciones = "Revisión ordinaria diaria" });
        _citas.Add(new Cita { Dni = "87654321X", VehiculoMatricula = "9999LMN", FechaInspeccion = DateTime.Now.AddDays(1), Estado = EstadoCita.Pendiente, Observaciones = "Vuelve por gases" });
    }

    // ─────────────────────────────────────────────────────────
    // COMANDO CON VALIDACIÓN EN CASCADA
    // ─────────────────────────────────────────────────────────
    [RelayCommand]
    private void AgregarCita()
    {
        ValidateAllProperties();

        if (HasErrors)
        {
            return;
        }

        var nuevaCita = new Cita
        {
            Dni = Dni,
            VehiculoMatricula = Matricula,
            FechaInspeccion = FechaInspeccion,
            Observaciones = Observaciones,
            Estado = EstadoCita.Pendiente
        };

        Citas.Add(nuevaCita);

        Dni = string.Empty;
        Matricula = string.Empty;
        Observaciones = string.Empty;
    }
}