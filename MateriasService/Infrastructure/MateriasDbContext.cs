using Microsoft.EntityFrameworkCore;
using MateriasService.Domain;

namespace MateriasService.Infrastructure;

// "Motor" de Entity Framework Core: conecta la clase Materia con la tabla en la base de datos.
public class MateriasDbContext : DbContext
{
    public MateriasDbContext(DbContextOptions<MateriasDbContext> options) : base(options) { }

    public DbSet<Materia> Materias => Set<Materia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Materia>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(m => m.Codigo).IsRequired().HasMaxLength(20);
            entity.HasIndex(m => m.Codigo).IsUnique();
        });
    }
}
