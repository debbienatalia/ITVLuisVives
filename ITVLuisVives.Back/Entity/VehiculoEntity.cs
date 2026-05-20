using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ITVLuisVives.Back.Entity;

[Table("Vehiculos")]
[Index(nameof(Matricula), IsUnique = true)]
[Index(nameof(Marca))]
[Index(nameof(Motor))]
[Index(nameof(FechaMatriculacion))]
[Index(nameof(IsDeleted))]
public class VehiculoEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string Matricula { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Marca { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Modelo { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Motor { get; set; } = string.Empty;

    [Column(TypeName = "datetime2")]
    public DateTime FechaMatriculacion { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column(TypeName = "datetime2")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool IsDeleted { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime? DeletedAt { get; set; }
}