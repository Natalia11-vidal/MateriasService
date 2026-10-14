using MateriasService.Common.Extensions;
using System.Security.Claims;

namespace MateriasService.Features.Materias.Delete;

// DELETE no recibe cuerpo: el id viaja en la ruta y no hay respuesta con datos (204).
public class DeleteMateriaEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/v1/materias/{id:int}", async (
            int id,                                  // id de la materia, viene de la URL
            ClaimsPrincipal user,                    // el token ya validado: de aquí sale el dueño
            ISender sender,                          // mensajero para mandarlo a su handler
            CancellationToken cancellationToken) =>  // freno de mano si el cliente cancela
        {
            var command = new DeleteMateriaCommand(user.GetUsuarioId(), id);
            await sender.Send(command, cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("DeleteMateria")
        .WithTags("Materias")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
