using MediatR;
using Mapster;
using Microsoft.EntityFrameworkCore;
using MateriasService.Infrastructure;

namespace MateriasService.Features.Materias.Get;

public static class GetMaterias
{
    public record MateriaDto(int Id, string Nombre, string Codigo, int Creditos, string Programa, string Estado);

    // --- Listar (con filtro opcional por programa) ---
    public record ListQuery(string? Programa) : IRequest<List<MateriaDto>>;

    public class ListHandler : IRequestHandler<ListQuery, List<MateriaDto>>
    {
        private readonly MateriasDbContext _db;
        public ListHandler(MateriasDbContext db) => _db = db;

        public async Task<List<MateriaDto>> Handle(ListQuery request, CancellationToken ct)
        {
            var query = _db.Materias.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(request.Programa))
                query = query.Where(m => m.Programa == request.Programa);

            return await query.ProjectToType<MateriaDto>().ToListAsync(ct);
        }
    }

    // --- Consultar por id ---
    public record ByIdQuery(int Id) : IRequest<MateriaDto?>;

    public class ByIdHandler : IRequestHandler<ByIdQuery, MateriaDto?>
    {
        private readonly MateriasDbContext _db;
        public ByIdHandler(MateriasDbContext db) => _db = db;

        public async Task<MateriaDto?> Handle(ByIdQuery request, CancellationToken ct)
        {
            var materia = await _db.Materias.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.Id, ct);
            return materia?.Adapt<MateriaDto>();
        }
    }

    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/materias", async (string? programa, ISender sender) =>
                Results.Ok(await sender.Send(new ListQuery(programa))))
            
            .WithName("ListMaterias")
            .WithTags("Materias");

        app.MapGet("/api/v1/materias/{id:int}", async (int id, ISender sender) =>
            {
                var materia = await sender.Send(new ByIdQuery(id));
                return materia is null ? Results.NotFound() : Results.Ok(materia);
            })
            
            .WithName("GetMateriaById")
            .WithTags("Materias");
    }
}
