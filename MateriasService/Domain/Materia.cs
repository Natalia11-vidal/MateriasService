namespace MateriasService.Domain;

// Clase de dominio: representa una materia en la base de datos.
// Coincide con los campos que definieron en el cuaderno: id, nombre, nrc/codigo, created_at.
public class Materia
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;   // NRC / código de la materia
    public int Creditos { get; set; }
    public string Programa { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Estado { get; set; } = "activa";       // activa / inactiva
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
