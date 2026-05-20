using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ITVLuisVives.Back.Entity;

[Table("Citas")]
[Index(nameof(Dni))]
[Index(nameof(VehiculoMatricula))]
[Index(nameof(FechaInspeccion))]
[Index(nameof(Estado))]
[Index(nameof(IsDeleted))]
public class CitaEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(9)]
    public string Dni { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string VehiculoMatricula { get; set; } = string.Empty;

    [Column(TypeName = "datetime2")]
    public DateTime FechaInspeccion { get; set; }

    [Required]
    [MaxLength(20)]
    public string Estado { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Observaciones { get; set; } = string.Empty;

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column(TypeName = "datetime2")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool IsDeleted { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime? DeletedAt { get; set; }
}