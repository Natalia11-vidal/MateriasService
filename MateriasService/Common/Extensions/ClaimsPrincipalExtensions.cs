using System.Security.Claims;

namespace MateriasService.Common.Extensions;

//Clase de .net para leer los ids autenticados
public static class ClaimsPrincipalExtensions
{
    // Lee el usuario_id del "sub" del token que generó auth.
    // Si no está o no es un número, no hay sesión válida → 401

    //es estatico no hay necesidad de instaciarlo con new
    //le decimos a c#  añada GetUsuarioId
    public static int GetUsuarioId(this ClaimsPrincipal user)
    {
        //Hay que obtener los clams los datos del suario que viajan con las credenciales
        //lo hace de lado a lado y si no lo deja en null
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(sub, out var usuarioId)
            ? usuarioId
            : throw new UnauthorizedAccessException("Token sin usuario válido");
    }
}