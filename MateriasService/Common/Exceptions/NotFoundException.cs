namespace MateriasService.Common.Exceptions;

// 404: la materia no existe o no es del usuario (contrato G.5)
public class NotFoundException(string message) : Exception(message);