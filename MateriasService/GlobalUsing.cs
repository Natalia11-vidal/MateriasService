// Librerías que usan todos los features
global using Carter;
global using FluentValidation;
global using Mapster;
global using MediatR;
global using Microsoft.EntityFrameworkCore;

// Capas propias del servicio
global using MateriasService.Domain;
global using MateriasService.Infrastructure;

// Common: firmas CQRS y excepciones que lanzan los handlers
global using MateriasService.Common.CQRS;
global using MateriasService.Common.Exceptions;