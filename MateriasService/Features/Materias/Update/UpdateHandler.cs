namespace MateriasService.Features.Materias.Update;

public record UpdateMateriaCommand(int UsuarioId, int MateriaId, string Nombre, string Nrc, string? Descripcion) : ICommand<UpdateMateriaResult>;

public record UpdateMateriaResult(int MateriaId, string Nombre, string Nrc, string? Descripcion, DateTime FechaCreacion);

internal class UpdateMateriaHandler(MateriasDbContext db) : ICommandHandler<UpdateMateriaCommand, UpdateMateriaResult>
{
    public async Task<UpdateMateriaResult> Handle(UpdateMateriaCommand command, CancellationToken cancellationToken)
    {
        var materia = await db.Materias
            .FirstOrDefaultAsync(m => m.Id == command.MateriaId && m.UsuarioId == command.UsuarioId, cancellationToken)
            ?? throw new NotFoundException("Materia no encontrada");

        var nrc = command.Nrc.Trim();

        // El NRC no puede repetirse entre las materias del mismo usuario (se excluye la propia).
        var yaExiste = await db.Materias.AnyAsync(
            m => m.UsuarioId == command.UsuarioId && m.Nrc == nrc && m.Id != command.MateriaId, cancellationToken);
        if (yaExiste) throw new ConflictException($"Ya tienes una materia con el NRC {nrc}");

        materia.Nombre = command.Nombre.Trim();
        materia.Nrc = nrc;
        materia.Descripcion = string.IsNullOrWhiteSpace(command.Descripcion) ? null : command.Descripcion.Trim();
        // FechaCreacion no se toca: es la fecha en que se creó la materia.

        await db.SaveChangesAsync(cancellationToken);

        return new UpdateMateriaResult(materia.Id, materia.Nombre, materia.Nrc, materia.Descripcion, materia.FechaCreacion);
    }
}
