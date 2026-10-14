using MateriasService.Common.Extensions;
using System.Security.Claims;

namespace MateriasService.Features.Materias.Create;

// Lo que manda el front: sin usuario_id, ese sale del token
public record CreateMateriaRequest(string Nombre, string Nrc, string? Descripcion);

// Lo que responde la API: los campos del contrato (G.4)
public record CreateMateriaResponse(
    int MateriaId,
    string Nombre,
    string Nrc,
    string? Descripcion,
    DateTime FechaCreacion);

// Carter lo descubre solo y lo registra con app.MapCarter()
public class CreateMateriaEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/materias", async (
               //lo que va crear proviene del token
                CreateMateriaRequest request,
                //guardar los Claims del token
                ClaimsPrincipal user,
                //mensajero para mandaralo su respectivo handler
                ISender sender,
                //freno de mano si sale todo mal
                CancellationToken cancellationToken) =>
        {
            // Request → Command; el dueño siempre sale del token
            var command = request.Adapt<CreateMateriaCommand>() with
            {
                UsuarioId = user.GetUsuarioId()
            };

            var result = await sender.Send(command, cancellationToken);

            // Result → Response
            //adaptamos el resultado del handler a la respuesta que espera el front
            //para separar Resposibilidades
            var response = result.Adapt<CreateMateriaResponse>();

            return Results.Created($"/api/v1/materias/{response.MateriaId}", response);
        })
            .RequireAuthorization()
            .WithName("CreateMateria")
            .WithTags("Materias")
            .Produces<CreateMateriaResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}