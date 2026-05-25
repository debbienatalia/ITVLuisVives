using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITVLuisVives.Back.Models;
using ITVLuisVives.Back.Enums;
using ITVLuisVives.Back.Services;
using ITVLuisVives.Back.Errors;
using Serilog;

namespace ITVLuisVives.Front.ViewModels;

public partial class CitasViewModel : ObservableValidator
{
    private readonly ICitaService _citaService;

    [ObservableProperty]
    private ObservableCollection<Cita> _citas = new();

    // ─────────────────────────────────────────────────────────
    // PROPIEDADES DEL FORMULARIO (ALTA DE NUEVA CITA)
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

    private string _horaCita = "10:00";
    [Required(ErrorMessage = "La hora es totalmente obligatoria.")]
    [RegularExpression(@"^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Formato inválido. Debe ser HH:mm (Ej: 14:30).")]
    public string HoraCita
    {
        get => _horaCita;
        set => SetProperty(ref _horaCita, value, true);
    }

    [ObservableProperty]
    private string _observaciones = string.Empty;

    // ─────────────────────────────────────────────────────────
    // PROPIEDADES DEL BUSCADOR (FILTRADO MULTI-PARAMÉTRICO)
    // ─────────────────────────────────────────────────────────
    [ObservableProperty]
    private string? _busquedaDni;

    [ObservableProperty]
    private string? _busquedaMatricula;

    [ObservableProperty]
    private DateTime? _busquedaFechaDesde;

    [ObservableProperty]
    private DateTime? _busquedaFechaHasta;

    // ─────────────────────────────────────────────────────────
    // PROPIEDADES DE PAGINACIÓN OBLIGATORIA (SERVIDOR)
    // ─────────────────────────────────────────────────────────
    [ObservableProperty]
    private int _paginaActual = 1;

    [ObservableProperty]
    private int _tamanoPagina = 10;

    [ObservableProperty]
    private bool _puedeIrPaginaAnterior;

    [ObservableProperty]
    private bool _puedeIrPaginaSiguiente;

    /// <summary>
    ///     Constructor del ViewModel. El contenedor de DI inyecta síncronamente el servicio del Back.
    /// </summary>
    public CitasViewModel(ICitaService citaService)
    {
        _citaService = citaService;
        
        EjecutarBusquedaSincrona();
    }

    // ─────────────────────────────────────────────────────────
    // COMANDO: ACCIÓN DEL BUSCADOR CON FILTROS Y PAGINACIÓN
    // ─────────────────────────────────────────────────────────
    [RelayCommand]
    private void EjecutarBusquedaSincrona()
    {
        Log.Information("Buscador: Consultando página {Pagina} en el servidor...", PaginaActual);
        try
        {
            string? dniFiltrar = string.IsNullOrWhiteSpace(BusquedaDni) ? null : BusquedaDni.Trim();
            string? matriculaFiltrar = string.IsNullOrWhiteSpace(BusquedaMatricula) ? null : BusquedaMatricula.Trim();

            var resultadosPaginados = _citaService.ObtenerFiltradas(
                dni: dniFiltrar,
                matricula: matriculaFiltrar,
                estado: null,
                fechaDesde: BusquedaFechaDesde,
                fechaHasta: BusquedaFechaHasta,
                pagina: PaginaActual,
                tamanoPagina: TamanoPagina
            );

            Citas = new ObservableCollection<Cita>(resultadosPaginados);

            PuedeIrPaginaAnterior = PaginaActual > 1;
            PuedeIrPaginaSiguiente = Citas.Count == TamanoPagina;

            Log.Information("Buscador: Se han renderizado {Count} elementos.", Citas.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error crítico síncrono en el filtrado de la base de datos.");
            MessageBox.Show("Ocurrió un error al consultar los datos del servidor.", "Buscador", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void LimpiarFiltros()
    {
        BusquedaDni = string.Empty;
        BusquedaMatricula = string.Empty;
        BusquedaFechaDesde = null;
        BusquedaFechaHasta = null;
        PaginaActual = 1;
        
        Log.Information("Buscador: Filtros limpiados por el usuario.");
        EjecutarBusquedaSincrona();
    }

    // ─────────────────────────────────────────────────────────
    // COMANDOS DE NAVEGACIÓN DE PAGINACIÓN
    // ─────────────────────────────────────────────────────────
    [RelayCommand]
    private void AvanzarPagina()
    {
        if (PuedeIrPaginaSiguiente)
        {
            PaginaActual++;
            EjecutarBusquedaSincrona();
        }
    }

    [RelayCommand]
    private void RetrocederPagina()
    {
        if (PuedeIrPaginaAnterior)
        {
            PaginaActual--;
            EjecutarBusquedaSincrona();
        }
    }

    // ─────────────────────────────────────────────────────────
    // COMANDO: AGREGAR NUEVA CITA (VALIDACIÓN EN CASCADA)
    // ─────────────────────────────────────────────────────────
    [RelayCommand]
    private void AgregarCita()
    {
        ValidateAllProperties();

        if (HasErrors)
        {
            Log.Warning("Formulario: No se puede guardar porque contiene errores visuales.");
            return;
        }

        TimeSpan timeSpan = TimeSpan.Parse(HoraCita);
        DateTime fechaYHoraCombinada = FechaInspeccion.Date + timeSpan;

        var nuevaCita = new Cita
        {
            Dni = Dni,
            VehiculoMatricula = Matricula,
            FechaInspeccion = fechaYHoraCombinada,
            Observaciones = Observaciones,
            Estado = EstadoCita.Pendiente
        };

        Log.Information("Formulario: Enviando cita con horario {Hora} al servicio para validación de negocio.", HoraCita);

        var resultado = _citaService.Agendar(nuevaCita);

        if (resultado.IsSuccess)
        {
            Log.Information("Formulario: Cita persistida con éxito en el Back.");
            
            EjecutarBusquedaSincrona();

            Dni = string.Empty;
            Matricula = string.Empty;
            Observaciones = string.Empty;
            FechaInspeccion = DateTime.Today;
            HoraCita = "10:00";
        }
        else
        {
            string mensajeError = resultado.Error?.Message ?? "Error desconocido en el dominio.";
            Log.Warning("Formulario: El Back rechazó la inserción: {Motivo}", mensajeError);

            MessageBox.Show(
                $"No se pudo agendar la cita por el siguiente motivo:\n\n• {mensajeError}", 
                "Restricción de Negocio", 
                MessageBoxButton.OK, 
                MessageBoxImage.Hand);
        }
    }
    
    // ─────────────────────────────────────────────────────────
    // COMANDO: CANCELAR/ELIMINAR CITA
    // ─────────────────────────────────────────────────────────
    [RelayCommand]
    private void EliminarCita(Cita citaParaEliminar)
    {
        if (citaParaEliminar == null) return;

        var resultadoUi = MessageBox.Show(
            $"¿Estás seguro de que deseas cancelar la cita del vehículo {citaParaEliminar.VehiculoMatricula}?",
            "Confirmar Cancelación", 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Warning);

        if (resultadoUi == MessageBoxResult.Yes)
        {
            Log.Information("Acciones: Solicitando cancelación síncrona de ID: {Id}", citaParaEliminar.Id);
            
            bool canceladoExitoso = _citaService.Cancelar(citaParaEliminar.Id, borradoLogico: true);

            if (canceladoExitoso)
            {
                Log.Information("Acciones: Cita dada de baja en el sistema.");
                EjecutarBusquedaSincrona();
            }
            else
            {
                Log.Error("Acciones: El Back devolvió error al cancelar {Id}.", citaParaEliminar.Id);
                MessageBox.Show("El sistema no pudo procesar la cancelación.", "Error de Persistencia", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}