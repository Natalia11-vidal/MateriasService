namespace MateriasService.Domain;

// Materia: espacio de organización propio del estudiante (nombre + NRC).
// Campos según el contrato de materias-service (sección G, v1.1).
public class Materia
{
    public int Id { get; set; }

    // Dueño: sale del "sub" del token. No es FK, porque los usuarios viven en otra base y otro servicio
    public int UsuarioId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    // Texto y no número, para conservar ceros a la izquierda
    public string Nrc { get; set; } = string.Empty;

    // Opcional: notas del estudiante (profesor, salón, horario...). Agregado en v1.1
    public string? Descripcion { get; set; }

    // Hora de Colombia (UTC-5); la asigna el handler al crear
    public DateTime FechaCreacion { get; set; }
}