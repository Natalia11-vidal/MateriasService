namespace MateriasService.Features.Materias.Create;

// --- Entrada: lo que manda el front + el dueño, que sale del token ---
public record CreateMateriaCommand(
    int UsuarioId,
    string Nombre,
    string Nrc,
    string? Descripcion) : ICommand<CreateMateriaResult>;

// --- Salida: los campos del contrato (G.4) ---
public record CreateMateriaResult(
    int MateriaId,
    string Nombre,
    string Nrc,
    string? Descripcion,
    DateTime FechaCreacion);

// --- Lógica ---
internal class CreateMateriaHandler(MateriasDbContext db)
    : ICommandHandler<CreateMateriaCommand, CreateMateriaResult>
{
    private static readonly TimeZoneInfo ZonaColombia =
        TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

    public async Task<CreateMateriaResult> Handle(
        CreateMateriaCommand command,
        CancellationToken cancellationToken)
    {
        var nrc = command.Nrc.Trim();

        // El NRC es único por estudiante: 409 si ya lo tiene
        var yaExiste = await db.Materias.AnyAsync(
            m => m.UsuarioId == command.UsuarioId && m.Nrc == nrc,
            cancellationToken);

        if (yaExiste)
            throw new ConflictException($"Ya tienes una materia con el NRC {nrc}");

        var materia = new Materia
        {
            UsuarioId = command.UsuarioId,
            Nombre = command.Nombre.Trim(),
            Nrc = nrc,
            Descripcion = string.IsNullOrWhiteSpace(command.Descripcion) ? null : command.Descripcion.Trim(),
            FechaCreacion = AhoraEnColombia()
        };

        db.Materias.Add(materia);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateMateriaResult(
            materia.Id,
            materia.Nombre,
            materia.Nrc,
            materia.Descripcion,
            materia.FechaCreacion);
    }

    // Hora de Colombia sin milisegundos: el contrato pide YYYY-MM-DDTHH:MM:SS
    private static DateTime AhoraEnColombia()
    {
        var ahora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaColombia);
        return ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));
    }
}