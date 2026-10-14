namespace MateriasService.Common.Exceptions;

// 409: el estudiante ya tiene una materia con ese NRC
public class ConflictException(string message) : Exception(message);