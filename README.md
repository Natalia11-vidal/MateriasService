# MateriasService — CampusStudy

Microservicio que administra las **materias personales** de cada estudiante. Cada estudiante crea sus materias (nombre + NRC); más adelante podrá invitar a otros estudiantes a través de **grupos-service**.

- **Puerto:** `8006`
- **Base de datos:** SQL Server (`MateriasDb`)
- **Autenticación:** JWT emitido por `users-auth` (HS256, secreto compartido)
- **Contrato:** sección **G** del documento *Contrato Microservicios CampusStudy*

---

## Tabla de contenido

1. [Stack](#1-stack)
2. [Arquitectura: Vertical Slice + CQRS](#2-arquitectura-vertical-slice--cqrs)
3. [Estructura de carpetas](#3-estructura-de-carpetas)
4. [Configuración](#4-configuración)
5. [Cómo correrlo](#5-cómo-correrlo)
6. [Migraciones con EF Core](#6-migraciones-con-ef-core)
7. [Endpoints](#7-endpoints)
8. [Formato de errores](#8-formato-de-errores)
9. [Cómo agregar un feature nuevo](#9-cómo-agregar-un-feature-nuevo)
10. [Flujo de Git](#10-flujo-de-git)
11. [Problemas comunes](#11-problemas-comunes)

---

## 1. Stack

| Paquete | Versión | Para qué |
|---|---|---|
| .NET | 8.0 | Runtime |
| EF Core SqlServer / Design / Tools | 8.0.10 | ORM y migraciones |
| MediatR | 14.x | Envía Commands/Queries a su Handler + pipeline de validación |
| FluentValidation (+ DependencyInjectionExtensions) | 12.x | Validaciones de entrada |
| Carter | 8.2.1 | Endpoints organizados por módulo (`ICarterModule`) |
| Mapster (+ DI) | 10.x | Mapeo Request → Command → Result → Response (`Adapt`) |
| JwtBearer | 8.0.10 | Validación del token |
| Swashbuckle | 6.6.2 | Swagger en desarrollo |

> **Carter 8.2.1**, no 10.x: Carter 10 es para .NET 10.
> `Microsoft.CodeAnalysis.CSharp` y `.Workspaces` 4.7.0 están instalados a propósito para evitar el conflicto **NU1107** entre Carter y EF Design.

---

## 2. Arquitectura: Vertical Slice + CQRS

Cada **feature** (caso de uso) vive en su propia carpeta con todo lo que necesita. No hay capas de "Services" ni "Repositories" compartidos.

```
Petición HTTP
   │
   ▼
Endpoint (Carter)          ← Recibe el Request, lee el usuario del token, Adapt → Command/Query
   │  sender.Send(...)
   ▼
ValidationBehavior         ← Pipeline de MediatR: ejecuta el Validator. Si falla → 400
   │
   ▼
Handler                    ← Lógica + EF Core. Lanza NotFound (404) / Conflict (409)
   │
   ▼
Result → Adapt → Response  ← El endpoint devuelve el Response (snake_case)
```

- **Command** = cambia datos (Create, Update, Delete) → `ICommand<TResponse>` / `ICommand`
- **Query** = solo lee (Get, GetById) → `IQuery<TResponse>`
- Las interfaces CQRS son propias (`Common/CQRS`) y por debajo heredan de MediatR.

### ¿De dónde sale el `usuario_id`?

**Nunca del body.** Sale del claim `sub` del JWT:

```csharp
var command = request.Adapt<CreateMateriaCommand>() with { UsuarioId = user.GetUsuarioId() };
```

`GetUsuarioId()` está en `Common/Extensions/ClaimsPrincipalExtensions.cs`. Así un estudiante no puede crear ni ver materias de otro.

---

## 3. Estructura de carpetas

```
MateriasService/
├── Common/
│   ├── Behaviors/ValidationBehavior.cs       # Ejecuta los validators antes del handler
│   ├── CQRS/                                 # ICommand, IQuery, ICommandHandler, IQueryHandler
│   ├── Exceptions/                           # NotFoundException (404), ConflictException (409)
│   ├── Extensions/ClaimsPrincipalExtensions.cs  # user.GetUsuarioId()
│   └── GlobalExceptionHandler.cs             # Convierte excepciones al formato del contrato
├── Domain/Materia.cs                         # Entidad
├── Features/Materias/
│   ├── Create/   CreateHandler.cs · CreateValidator.cs · CreateEndpoint.cs
│   ├── Get/      GetHandler.cs · GetEndpoint.cs        (sin validator: no hay entrada)
│   ├── GetById/  (pendiente)
│   ├── Update/   (pendiente)
│   └── Delete/   (pendiente)
├── Infrastructure/MateriasDbContext.cs       # DbContext + índices
├── Migrations/                               # ⚠️ SE VERSIONA EN GIT, NO SE IGNORA
├── GlobalUsing.cs
├── Program.cs
├── appsettings.json
├── Dockerfile.dev
└── .dockerignore
```

### Regla del índice único

```csharp
entity.HasIndex(m => new { m.UsuarioId, m.Nrc }).IsUnique();
```

El **NRC es único por estudiante**: un estudiante no puede tener dos materias con el mismo NRC, pero dos estudiantes distintos sí pueden registrar el mismo NRC (cada uno tiene su copia personal). El nombre es libre.

---

## 4. Configuración

| Clave | Variable de entorno (Docker) | Valor local por defecto | Descripción |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | LocalDB | Cadena de conexión a SQL Server |
| `Jwt:Key` | `Jwt__Key` | (en `appsettings.json`) | **Mismo secreto** que usa `users-auth` para firmar |

> En variables de entorno, el `:` de .NET se escribe como **doble guion bajo** `__`.

### Sobre el JWT

- Algoritmo **HS256**, secreto compartido con `users-auth`.
- **El secreto debe tener al menos 32 caracteres** (256 bits). Con uno más corto aparece `IDX10720`.
- No se valida `Issuer` ni `Audience` (auth no los emite).
- `MapInboundClaims = false` para que el claim `sub` llegue como `sub` y no se renombre.
- En Docker el valor viene de `JWT_SECRET` del `.env` de `infra-campusstudy`. **Si el secreto de materias y el de auth no coinciden, todo responde 401.**

---

## 5. Cómo correrlo

### Opción A — Docker (recomendado, todo el sistema)

Desde el repo `infra-campusstudy`:

```bash
docker compose up -d
docker compose logs -f materias-service
```

Levanta `sqlserver-materias` (SQL Server 2022) y `materias-service` con **`dotnet watch`**: al guardar un `.cs`, el contenedor recompila solo.

| Acción | Comando |
|---|---|
| Ver logs | `docker compose logs -f materias-service` |
| Reiniciar | `docker compose restart materias-service` |
| **Agregaste/actualizaste un paquete NuGet** | `docker compose up -d --build --force-recreate -V materias-service` |
| Estado de los contenedores | `docker compose ps` |

> El `-V` recrea los volúmenes anónimos `/app/bin` y `/app/obj`. Sin él, el contenedor sigue usando el restore viejo y salen errores como *"The type or namespace 'Carter' could not be found"*.

### Opción B — Local con Visual Studio (LocalDB)

1. Abre `MateriasService.sln`.
2. Aplica las migraciones a tu LocalDB (ver sección 6).
3. Ejecuta con el perfil **http** o **https**.
4. Swagger: `http://localhost:<puerto>/swagger`.

---

## 6. Migraciones con EF Core

### Requisito: herramienta `dotnet ef`

```bash
dotnet ef --version
# si no existe:
dotnet tool install --global dotnet-ef --version 8.*
# si existe pero es vieja:
dotnet tool update --global dotnet-ef --version 8.*
```

> Todos los comandos `dotnet ef` se ejecutan **dentro de la carpeta del proyecto** (donde está `MateriasService.csproj`), no en la carpeta de la solución.
> Equivalente en Visual Studio: *Herramientas → Administrador de paquetes NuGet → Consola del Administrador de paquetes*.

### Comandos

| Qué quieres hacer | CLI (`dotnet ef`) | Consola de VS |
|---|---|---|
| Crear una migración | `dotnet ef migrations add NombreDelCambio` | `Add-Migration NombreDelCambio` |
| Aplicarla a tu base local | `dotnet ef database update` | `Update-Database` |
| Ver la lista de migraciones | `dotnet ef migrations list` | `Get-Migration` |
| Quitar la **última** migración (si **no** se ha aplicado) | `dotnet ef migrations remove` | `Remove-Migration` |
| Volver la base a una migración anterior | `dotnet ef database update NombreMigracion` | `Update-Database NombreMigracion` |
| Ver el SQL que se va a ejecutar | `dotnet ef migrations script` | `Script-Migration` |
| Borrar la base local completa | `dotnet ef database drop` | `Drop-Database` |

### Flujo normal al cambiar el modelo

```bash
# 1. Cambias Domain/Materia.cs o MateriasDbContext.cs
# 2. Compilas: debe dar 0 errores (EF compila el proyecto para generar la migración)
dotnet build
# 3. Creas la migración con un nombre que diga QUÉ cambió
dotnet ef migrations add AgregarColorMateria
# 4. ABRES el archivo generado y revisas Up() y Down()
# 5. Local: aplicas
dotnet ef database update
# 6. Docker: no hay que hacer nada → dotnet watch reinicia y Database.Migrate() la aplica
```

### ¿Cómo se aplican en Docker?

`Program.cs`, en ambiente **Development**, ejecuta al arrancar:

```csharp
using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<MateriasDbContext>().Database.Migrate();
```

`Migrate()` revisa la tabla `__EFMigrationsHistory` y aplica **solo las migraciones que falten**. Por eso en los logs del contenedor debe aparecer:

```
Applying migration '2026xxxxxxxxxx_NombreDelCambio'.
```

### ⚠️ Consideraciones importantes

1. **La carpeta `Migrations/` se sube a Git y NO va en `.gitignore` ni en `.dockerignore`.** El contenedor necesita las migraciones compiladas para crear las tablas, y el equipo necesita el mismo historial.
2. **Nunca edites ni borres una migración que ya está en `develop`.** Si algo quedó mal, crea **otra** migración que lo corrija. Editar una ya aplicada deja las bases de tus compañeras desincronizadas.
3. **`migrations remove` solo para la última y solo si no la has aplicado ni subido.** Si ya la aplicaste localmente: primero `dotnet ef database update <MigracionAnterior>` y luego `remove`.
4. **Una migración por cambio y con nombre descriptivo:** `AgregarColorMateria`, no `Cambios` ni `Migracion2`.
5. **Revisa siempre el archivo generado.** Fíjate especialmente en `DropColumn` / `DropTable`: borran datos.
6. **No toques `MateriasDbContextModelSnapshot.cs` a mano.** EF lo actualiza solo; es la "foto" actual del modelo.
7. **Conflictos de migraciones entre ramas:** si dos personas crean migraciones en paralelo, al hacer merge el `ModelSnapshot` choca. Solución: quien hace merge de segundo borra **su** migración (sin aplicar), hace pull de `develop` y la vuelve a generar.
8. **Coordinen los cambios de modelo.** Avisen en el grupo antes de crear una migración para no pisarse.

### Reset de la base (solo desarrollo)

Necesario cuando se reinicia el historial de migraciones (como pasó al alinear el modelo con el contrato: se borraron las migraciones viejas y se creó un `InitialCreate` nuevo).

**LocalDB (Visual Studio):**

```bash
dotnet ef database drop      # confirma con "y"
dotnet ef database update
```

**Docker (solo la base de materias):**

```bash
# desde infra-campusstudy
docker compose stop materias-service sqlserver-materias
docker compose rm -f sqlserver-materias
docker volume ls | grep mssql                       # ver el nombre exacto del volumen
docker volume rm infra-campusstudy_mssql-materias-data
docker compose up -d sqlserver-materias materias-service
docker compose logs -f materias-service            # esperar "Applying migration ..."
```

> 🚫 **No uses `docker compose down -v`**: borra **todos** los volúmenes, incluida la base de `users-auth` (Postgres), y se pierden los usuarios registrados.

### Consultar la base del contenedor

```bash
docker compose exec sqlserver-materias /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P '<MATERIAS_DB_PASSWORD>' -C \
  -Q "SELECT * FROM MateriasDb.dbo.Materias"
```

O conéctate con **SQL Server Management Studio / Azure Data Studio** a `localhost,1433`, usuario `sa`, con *Trust server certificate* activado.

---

## 7. Endpoints

Todos requieren `Authorization: Bearer <token>`. Las respuestas salen en **snake_case**.

| Método | Ruta | Estado | Respuestas |
|---|---|---|---|
| `POST` | `/api/v1/materias` | ✅ Hecho | 201 · 400 · 401 · 409 |
| `GET` | `/api/v1/materias` | ✅ Hecho | 200 · 401 |
| `GET` | `/api/v1/materias/{materia_id}` | ⏳ Pendiente | 200 · 401 · 404 |
| `PUT` | `/api/v1/materias/{materia_id}` | ⏳ Pendiente | 200 · 400 · 401 · 404 · 409 |
| `DELETE` | `/api/v1/materias/{materia_id}` | ⏳ Pendiente | 200 · 401 · 404 |

### POST `/api/v1/materias`

```json
// Request
{ "nombre": "Sistemas Distribuidos", "nrc": "12345", "descripcion": "Salón 302" }

// 201 Created
{
  "materia_id": 1,
  "nombre": "Sistemas Distribuidos",
  "nrc": "12345",
  "descripcion": "Salón 302",
  "fecha_creacion": "2026-10-14T10:30:05"
}
```

| Campo | Regla |
|---|---|
| `nombre` | Obligatorio, máx. 100 |
| `nrc` | Obligatorio, máx. 20, único por estudiante |
| `descripcion` | Opcional (`null`), máx. 500 — *contrato v1.1* |

`fecha_creacion` se guarda en **hora de Colombia** (`America/Bogota`), sin milisegundos.

### GET `/api/v1/materias`

Devuelve un **arreglo plano** con las materias del usuario del token, de la más reciente a la más antigua. Si no tiene materias → `200 []`.

---

## 8. Formato de errores

Lo arma `GlobalExceptionHandler` según el contrato:

| Excepción | Código | Body |
|---|---|---|
| `ValidationException` (FluentValidation) | 400 | `{ "error": "Datos de entrada inválidos", "errors": [{ "campo": "nrc", "mensaje": "..." }] }` |
| `UnauthorizedAccessException` | 401 | `{ "error": "No autorizado" }` |
| `NotFoundException` | 404 | `{ "error": "<mensaje>" }` |
| `ConflictException` | 409 | `{ "error": "<mensaje>" }` |
| Cualquier otra | 500 | `{ "error": "Error interno del servidor" }` |

> Si falta el token o la firma es inválida, el 401 lo responde directamente el middleware JwtBearer (sin body).

**En los handlers no se devuelven códigos HTTP**: se lanza la excepción y el handler global la traduce.

```csharp
throw new NotFoundException($"La materia {id} no existe");
throw new ConflictException($"Ya tienes una materia con el NRC {nrc}");
```

---

## 9. Cómo agregar un feature nuevo

Ejemplo: **GetById**. Cada feature va en su carpeta, con **un archivo por responsabilidad**.

```
Features/Materias/GetById/
├── GetByIdHandler.cs     # Query/Command + Result + Handler
├── GetByIdValidator.cs   # Solo si hay datos de entrada que validar
└── GetByIdEndpoint.cs    # Request/Response + ICarterModule
```

### Reglas

| Elemento | Visibilidad | Nota |
|---|---|---|
| Query / Command / Result | `public record` | |
| Handler | `internal class` | Inyecta `MateriasDbContext` con constructor primario |
| Validator | `internal class : AbstractValidator<...>` | Se registra solo (`includeInternalTypes: true`) |
| Endpoint | **`public class : ICarterModule`** | Carter solo descubre clases **públicas** |
| Request / Response | `public record` | |

### Checklist

- [ ] `UsuarioId` sale del token (`user.GetUsuarioId()`), nunca del body ni de la URL.
- [ ] Toda consulta filtra por `UsuarioId` → un estudiante **solo** ve/edita/borra lo suyo. Si la materia existe pero es de otro, responder **404** (no 403), para no revelar que existe.
- [ ] Las queries usan `AsNoTracking()`.
- [ ] No existe → `throw new NotFoundException(...)`.
- [ ] NRC repetido (Update) → `ConflictException`, **excluyendo la propia materia** (`m.Id != command.MateriaId`).
- [ ] Endpoint con `.RequireAuthorization()`, `.WithName(...)`, `.WithTags("Materias")`, `.Produces...`.
- [ ] Ruta con restricción de tipo: `/api/v1/materias/{materiaId:int}`.
- [ ] Mapeo con Mapster: `request.Adapt<Command>() with { UsuarioId = ..., MateriaId = ... }`.
- [ ] Probado en Swagger/Postman con token real (login por el gateway).

### Esqueleto de referencia (Handler)

```csharp
namespace MateriasService.Features.Materias.GetById;

public record GetMateriaByIdQuery(int UsuarioId, int MateriaId) : IQuery<GetMateriaByIdResult>;

public record GetMateriaByIdResult(int MateriaId, string Nombre, string Nrc, string? Descripcion, DateTime FechaCreacion);

internal class GetMateriaByIdHandler(MateriasDbContext db)
    : IQueryHandler<GetMateriaByIdQuery, GetMateriaByIdResult>
{
    public async Task<GetMateriaByIdResult> Handle(GetMateriaByIdQuery query, CancellationToken cancellationToken)
    {
        var materia = await db.Materias.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == query.MateriaId && m.UsuarioId == query.UsuarioId, cancellationToken)
            ?? throw new NotFoundException($"La materia {query.MateriaId} no existe");

        return new GetMateriaByIdResult(materia.Id, materia.Nombre, materia.Nrc, materia.Descripcion, materia.FechaCreacion);
    }
}
```

Usa `Create/` y `Get/` como guía para el Validator y el Endpoint.

---

## 10. Flujo de Git

| Rama | Uso |
|---|---|
| `master` | Estable. No se trabaja directo aquí. |
| `develop` | Integración. **Aquí está la arquitectura nueva** (CQRS, Carter, MediatR...). |
| `feature/<tarea>` | Una rama por tarea, **creada desde `develop`** |

```bash
git checkout develop
git pull
git checkout -b feature/materias-getbyid
# ... trabajo ...
git push -u origin feature/materias-getbyid
# Pull Request → develop
```

> ⚠️ Si creas tu rama desde `master` no vas a tener `Common/`, Carter ni MediatR, y el merge va a chocar.
> Las ramas se nombran **por tarea**, no por persona.

Después de hacer pull de `develop` por primera vez con las migraciones nuevas → [Reset de la base](#reset-de-la-base-solo-desarrollo).

---

## 11. Problemas comunes

| Síntoma | Causa | Solución |
|---|---|---|
| **401** con un token recién sacado | `Jwt__Key` distinto al secreto de auth | Revisar `JWT_SECRET` en el `.env` de infra |
| `IDX10720` | Secreto JWT de menos de 32 caracteres | Usar uno de 32+ |
| `There is already an object named 'Materias'` | La base tiene un historial de migraciones viejo | [Reset de la base](#reset-de-la-base-solo-desarrollo) |
| `Invalid column name 'UsuarioId'` / `'Descripcion'` | Migración sin aplicar | `dotnet ef database update` o revisar los logs del contenedor |
| `The type or namespace 'Carter' could not be found` (en Docker) | Restore viejo en el volumen `/app/obj` | `docker compose up -d --build --force-recreate -V materias-service` |
| `NU1107` al instalar Carter | Conflicto de versiones de Roslyn | Instalar `Microsoft.CodeAnalysis.CSharp` y `.Workspaces` 4.7.0 |
| El endpoint no aparece en Swagger | La clase del endpoint es `internal` | Debe ser `public class ... : ICarterModule` |
| El validator no se ejecuta | No hereda de `AbstractValidator<TuCommand>` correcto | Revisar el tipo genérico |
| `Login failed for user 'sa'` en el primer arranque | SQL Server todavía inicializando | Normal: el healthcheck espera y se resuelve solo |
| `No executable found matching command "dotnet-ef"` | Falta la herramienta | `dotnet tool install --global dotnet-ef --version 8.*` |
| `Unable to create a 'DbContext'` al crear la migración | El proyecto no compila o estás en la carpeta equivocada | `dotnet build` y ejecutar dentro de la carpeta del `.csproj` |