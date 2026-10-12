using MediatR;
using Microsoft.EntityFrameworkCore;
using MateriasService.Infrastructure;

namespace MateriasService.Features.Materias.Delete;

public static class DeleteMateria
{
    public record Command(int Id) : IRequest<bool>;

    public class Handler : IRequestHandler<Command, bool>
    {
        private readonly MateriasDbContext _db;
        public Handler(MateriasDbContext db) => _db = db;

        public async Task<bool> Handle(Command request, CancellationToken ct)
        {
            var materia = await _db.Materias.FirstOrDefaultAsync(m => m.Id == request.Id, ct);
            if (materia is null) return false;

            _db.Materias.Remove(materia);
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/v1/materias/{id:int}", async (int id, ISender sender) =>
        {
            var eliminado = await sender.Send(new Command(id));
            return eliminado ? Results.NoContent() : Results.NotFound();
        })
        
        .WithName("DeleteMateria")
        .WithTags("Materias");
    }
}
