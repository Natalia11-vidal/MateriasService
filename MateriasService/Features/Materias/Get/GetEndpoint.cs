using MateriasService.Common.Extensions;
using System.Security.Claims;

namespace MateriasService.Features.Materias.Get;

// Lo que responde la API: los campos del contrato (G.4)
public record GetMateriasResponse(
    int MateriaId,
    string Nombre,
    string Nrc,
    string? Descripcion,
    DateTime FechaCreacion);

public class GetMateriasEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/materias", async (
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken cancellationToken) =>
        {
            // El único dato de entrada es el usuario del token
            var query = new GetMateriasQuery(user.GetUsuarioId());

            var result = await sender.Send(query, cancellationToken);

            // Result → Response (lista)
            var response = result.Adapt<List<GetMateriasResponse>>();

            return Results.Ok(response);
        })
            .RequireAuthorization()
            .WithName("GetMaterias")
            .WithTags("Materias")
            .Produces<List<GetMateriasResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}