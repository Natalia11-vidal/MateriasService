namespace MateriasService.Infrastructure;

// Conecta la clase Materia con la tabla en la base de datos.
// Solo se configura lo que EF no deduce por convención: largos e índices.
public class MateriasDbContext(DbContextOptions<MateriasDbContext> options) : DbContext(options)
{
    public DbSet<Materia> Materias => Set<Materia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Materia>(entity =>
        {
            // Largos según el contrato (G.6)
            entity.Property(m => m.Nombre).HasMaxLength(100);
            entity.Property(m => m.Nrc).HasMaxLength(20);
            entity.Property(m => m.Descripcion).HasMaxLength(500);

            // "Mis materias" filtra por usuario: índice para que la consulta sea rápida
            entity.HasIndex(m => m.UsuarioId);

            // Un estudiante no repite NRC; dos estudiantes distintos sí pueden tener el mismo
            entity.HasIndex(m => new { m.UsuarioId, m.Nrc }).IsUnique();
        });
    }
}