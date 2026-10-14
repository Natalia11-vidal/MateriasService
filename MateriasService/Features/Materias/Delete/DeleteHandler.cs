namespace MateriasService.Features.Materias.Delete;

public record DeleteMateriaCommand(int UsuarioId, int MateriaId) : ICommand<DeleteMateriaResult>;

public record DeleteMateriaResult(int MateriaId);

internal class DeleteMateriaHandler(MateriasDbContext db) : ICommandHandler<DeleteMateriaCommand, DeleteMateriaResult>
{
    public async Task<DeleteMateriaResult> Handle(DeleteMateriaCommand command, CancellationToken cancellationToken)
    {
        // Se busca por Id Y por dueño: si la materia es de otro usuario se responde 404 igual que si no existiera.
        var materia = await db.Materias
            .FirstOrDefaultAsync(m => m.Id == command.MateriaId && m.UsuarioId == command.UsuarioId, cancellationToken)
            ?? throw new NotFoundException("Materia no encontrada");

        db.Materias.Remove(materia);
        await db.SaveChangesAsync(cancellationToken);

        return new DeleteMateriaResult(command.MateriaId);
    }
}
