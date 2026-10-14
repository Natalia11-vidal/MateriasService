namespace MateriasService.Features.Materias.Get;

// --- Entrada: solo necesita saber quién pregunta (sale del token) ---
public record GetMateriasQuery(int UsuarioId) : IQuery<IReadOnlyList<GetMateriasResult>>;

// --- Salida: los campos del contrato (G.4) ---
public record GetMateriasResult(
    int MateriaId,
    string Nombre,
    string Nrc,
    string? Descripcion,
    DateTime FechaCreacion);

// --- Lógica: mis materias, las más recientes primero ---
internal class GetMateriasHandler(MateriasDbContext db)
    : IQueryHandler<GetMateriasQuery, IReadOnlyList<GetMateriasResult>>
{
    public async Task<IReadOnlyList<GetMateriasResult>> Handle(
        GetMateriasQuery query, CancellationToken cancellationToken)
    {
        return await db.Materias
            .AsNoTracking()
            .Where(m => m.UsuarioId == query.UsuarioId)
            .OrderByDescending(m => m.FechaCreacion)
            .Select(m => new GetMateriasResult(m.Id, m.Nombre, m.Nrc, m.Descripcion, m.FechaCreacion))
            .ToListAsync(cancellationToken);
    }
}