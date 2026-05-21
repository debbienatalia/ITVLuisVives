using Microsoft.EntityFrameworkCore;

namespace ITVLuisVives.Back.Entity;

/// <summary>
///     Contexto de Entity Framework Core para la base de datos de la ITV.
/// </summary>
public class ItvDbContext : DbContext 
{
    private readonly string _connectionString;

    public ItvDbContext(string connectionString) 
    {
        _connectionString = connectionString;
    }

    public ItvDbContext(DbContextOptions<ItvDbContext> options) : base(options) 
    {
        _connectionString = "";
    }

    public DbSet<VehiculoEntity> Vehiculos { get; set; } = null!;
    public DbSet<CitaEntity> Citas { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
    {
        if (!optionsBuilder.IsConfigured) 
        {
            optionsBuilder.UseSqlite(_connectionString);
        }
    }

    public void EnsureCreated() 
    {
        Database.EnsureCreated();
    }
}