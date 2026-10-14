using MateriasService.Common.Extensions;
using System.Security.Claims;

namespace MateriasService.Features.Materias.Update;

// El id va en la ruta, por eso el cuerpo solo trae los campos editables.
public record UpdateMateriaRequest(string Nombre, string Nrc, string? Descripcion);

public record UpdateMateriaResponse(int MateriaId, string Nombre, string Nrc, string? Descripcion, DateTime FechaCreacion);

public class UpdateMateriaEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/materias/{id:int}", async (
            int id,                                  // id de la materia, viene de la URL
            UpdateMateriaRequest request,            // cuerpo JSON con los nuevos datos
            ClaimsPrincipal user,                    // el token ya validado: de aquí sale el dueño
            ISender sender,                          // mensajero para mandarlo a su handler
            CancellationToken cancellationToken) =>  // freno de mano si el cliente cancela
        {
            var command = request.Adapt<UpdateMateriaCommand>() with
            {
                UsuarioId = user.GetUsuarioId(),
                MateriaId = id
            };
            var result = await sender.Send(command, cancellationToken);
            var response = result.Adapt<UpdateMateriaResponse>();
            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("UpdateMateria")
        .WithTags("Materias")
        .Produces<UpdateMateriaResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
