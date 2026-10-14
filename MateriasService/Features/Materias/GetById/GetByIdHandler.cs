namespace MateriasService.Features.Materias.GetById;

public record GetMateriaByIdQuery(int UsuarioId, int MateriaId) : IQuery<GetMateriaByIdResult>;

public record GetMateriaByIdResult(int MateriaId, string Nombre, string Nrc, string? Descripcion, DateTime FechaCreacion);

internal class GetMateriaByIdHandler(MateriasDbContext db) : IQueryHandler<GetMateriaByIdQuery, GetMateriaByIdResult>
{
    public async Task<GetMateriaByIdResult> Handle(GetMateriaByIdQuery query, CancellationToken cancellationToken)
    {
        var materia = await db.Materias
            .AsNoTracking()
            .Where(m => m.Id == query.MateriaId && m.UsuarioId == query.UsuarioId)
            .Select(m => new GetMateriaByIdResult(m.Id, m.Nombre, m.Nrc, m.Descripcion, m.FechaCreacion))
            .FirstOrDefaultAsync(cancellationToken);

        return materia ?? throw new NotFoundException("Materia no encontrada");
    }
}
