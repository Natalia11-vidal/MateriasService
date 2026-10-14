using MateriasService.Common;
using MateriasService.Common.Behaviors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// --- Entity Framework Core ---
builder.Services.AddDbContext<MateriasDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- MediatR: Commands/Queries + validación automática antes de cada handler ---
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// --- FluentValidation (registra todos los validadores del proyecto) ---
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// --- Mapster (mapeo Domain <-> DTO) ---
builder.Services.AddMapster();

// --- Carter (descubre los endpoints de cada feature) ---
builder.Services.AddCarter();

// --- JSON en snake_case, como el contrato: materia_id, fecha_creacion ---
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);

// --- Manejo global de errores con el formato del contrato: { error, errors[] } ---
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// --- JWT: valida los tokens que genera users-auth (HS256 + JWT_SECRET compartido) ---
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Key");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Deja los claims con su nombre original: "sub" sigue siendo "sub"
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Lo que SÍ se valida
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,

            // Auth no pone emisor ni audiencia en sus tokens
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
});

var app = builder.Build();

// --- Primero: captura cualquier excepción de lo que viene después ---
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Crea o actualiza las tablas al arrancar
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<MateriasDbContext>().Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

// --- Endpoints: Carter registra todos los ICarterModule del proyecto ---
app.MapCarter();

app.Run();