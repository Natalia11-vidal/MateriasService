using MediatR;
using Microsoft.EntityFrameworkCore;
using MateriasService.Infrastructure;

namespace MateriasService.Features.Materias.Update;

public static class UpdateMateria
{
    public record Command(
        int Id,
        string Nombre,
        string Codigo,
        int Creditos,
        string Programa,
        string? Descripcion) : IRequest<Result?>;

    public record Result(int Id, string Nombre, string Codigo, int Creditos, string Programa);

    public class Handler : IRequestHandler<Command, Result?>
    {
        private readonly MateriasDbContext _db;
        public Handler(MateriasDbContext db) => _db = db;

        public async Task<Result?> Handle(Command request, CancellationToken ct)
        {
            var materia = await _db.Materias.FirstOrDefaultAsync(m => m.Id == request.Id, ct);
            if (materia is null) return null; // el endpoint traduce esto a 404

            materia.Nombre = request.Nombre;
            materia.Codigo = request.Codigo;
            materia.Creditos = request.Creditos;
            materia.Programa = request.Programa;
            materia.Descripcion = request.Descripcion;

            await _db.SaveChangesAsync(ct);

            return new Result(materia.Id, materia.Nombre, materia.Codigo, materia.Creditos, materia.Programa);
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/materias/{id:int}", async (int id, Command body, ISender sender) =>
        {
            if (id != body.Id) return Results.BadRequest("El id de la ruta no coincide con el del cuerpo.");

            var result = await sender.Send(body);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
       
        .WithName("UpdateMateria")
        .WithTags("Materias");
    }
}
