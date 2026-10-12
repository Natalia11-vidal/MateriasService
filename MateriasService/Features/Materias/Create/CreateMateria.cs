using MediatR;
using Mapster;
using MateriasService.Domain;
using MateriasService.Infrastructure;

namespace MateriasService.Features.Materias.Create;

// Todo lo relacionado a "crear una materia" vive en este único archivo:
// el Command (entrada), el Result (salida), el Handler (lógica) y el Endpoint.
public static class CreateMateria
{
    public record Command(
        string Nombre,
        string Codigo,
        int Creditos,
        string Programa,
        string? Descripcion) : IRequest<Result>;

    public record Result(
        int Id,
        string Nombre,
        string Codigo,
        int Creditos,
        string Programa,
        string Estado);

    public class Handler : IRequestHandler<Command, Result>
    {
        private readonly MateriasDbContext _db;
        public Handler(MateriasDbContext db) => _db = db;

        public async Task<Result> Handle(Command request, CancellationToken ct)
        {
            var materia = request.Adapt<Materia>();
            materia.Estado = "activa";
            materia.CreatedAt = DateTime.UtcNow;

            _db.Materias.Add(materia);
            await _db.SaveChangesAsync(ct);

            return materia.Adapt<Result>();
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/materias", async (Command command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Created($"/api/v1/materias/{result.Id}", result);
        })
        
        .WithName("CreateMateria")
        .WithTags("Materias");
    }
}
