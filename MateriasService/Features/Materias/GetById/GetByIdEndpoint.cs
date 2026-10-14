using MateriasService.Common.Extensions;
using System.Security.Claims;

namespace MateriasService.Features.Materias.GetById;

public record GetMateriaByIdResponse(int MateriaId, string Nombre, string Nrc, string? Descripcion, DateTime FechaCreacion);

public class GetMateriaByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/materias/{id:int}", async (
            int id,                                  // id de la materia, viene de la URL
            ClaimsPrincipal user,                    // el token ya validado: de aquí sale el dueño
            ISender sender,                          // mensajero para mandarlo a su handler
            CancellationToken cancellationToken) =>  // freno de mano si el cliente cancela
        {
            var query = new GetMateriaByIdQuery(user.GetUsuarioId(), id);
            var result = await sender.Send(query, cancellationToken);
            var response = result.Adapt<GetMateriaByIdResponse>();
            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("GetMateriaById")
        .WithTags("Materias")
        .Produces<GetMateriaByIdResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
