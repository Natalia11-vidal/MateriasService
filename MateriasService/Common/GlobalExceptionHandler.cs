using MateriasService.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;

namespace MateriasService.Common;

// Convierte cualquier excepción en el formato de error del contrato: { error, errors[] }
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, error) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Datos de entrada inválidos"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "No autorizado"),
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor")
        };

        // Solo los 500 son errores reales; el resto son respuestas esperadas
        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Excepción no controlada");
        else
            logger.LogWarning("{Status}: {Message}", statusCode, exception.Message);

        httpContext.Response.StatusCode = statusCode;

        object body = exception is ValidationException validationException
            ? new
            {
                error,
                errors = validationException.Errors.Select(e => new
                {
                    // "Nombre" → "nombre", igual que el JSON del contrato
                    campo = JsonNamingPolicy.SnakeCaseLower.ConvertName(e.PropertyName),
                    mensaje = e.ErrorMessage
                })
            }
            : new { error };

        await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);
        return true;
    }
}