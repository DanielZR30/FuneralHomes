using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using FH.Customers.Application;
using FH.Customers.Infrastructure;
using FH.Customers.Persistence;
using FH.Customers.Persistence.Seeds;

var builder = WebApplication.CreateBuilder(args);

// Capas de Clean Architecture (cada una se registra con su propio método de extensión)
builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// Autenticación: valida los JWT emitidos por Identity (fh.api.identity). Contrato compartido:
// issuer fh.api.identity, audiencia fh-apis, HS256, ClockSkew cero (token expirado falla al segundo).
// RoleClaimType "role" + MapInboundClaims false para que [Authorize(Roles=...)] lea el claim "role" del JWT.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role",
            NameClaimType = "name"
        };
    });

builder.Services.AddAuthorization();

// Controladores (Presentation). Enums como texto legible (ej: "Individual", "Human", "Child").
// SuppressImplicitRequired...: no exigir campos "no nulos" a nivel de framework; las reglas las valida la aplicación.
builder.Services.AddControllers(options =>
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Configuración de Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Funeral Homes - Customers Microservice API",
        Version = "v1",
        Description = "Microservicio para la gestión de titulares (B2C/B2B), suscripciones a planes y beneficiarios (fh.api.customer)."
    });
    // Soporte de Bearer: pegar el token de POST /api/v1/auth/login de Identity para probar los endpoints protegidos
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega aquí el accessToken devuelto por POST /api/v1/auth/login de Identity (sin la palabra Bearer)."
    });
    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

var app = builder.Build();

// Datos iniciales de desarrollo (cliente y suscripción de ejemplo)
if (app.Environment.IsDevelopment())
{
    await DataBaseSeeder.SeedAsync(app.Services);
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Funeral Homes Customers API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

// Manejo centralizado de excepciones con ProblemDetails (RN-09, BusinessRule -> 422, Validation -> 400, NotFound -> 404)
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var ex = exceptionHandlerPathFeature?.Error;

        var statusCode = ex switch
        {
            FH.Customers.Domain.Exceptions.BusinessRuleException => StatusCodes.Status422UnprocessableEntity,
            FluentValidation.ValidationException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        if (ex is FluentValidation.ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

            var validationProblem = new Microsoft.AspNetCore.Mvc.ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Error de Validación",
                Detail = string.Join(" ", errors.Values.SelectMany(m => m).Distinct())
            };

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(validationProblem);
            return;
        }

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = ex switch
            {
                FH.Customers.Domain.Exceptions.BusinessRuleException => "Regla de Negocio Incumplida",
                KeyNotFoundException => "Recurso No Encontrado",
                _ => "Error Interno del Servidor"
            },
            Detail = ex?.Message
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

// Autenticación (JWT de Identity) y autorización (roles Admin/Operator/Auditor en los controllers)
app.UseAuthentication();
app.UseAuthorization();

// Redireccionar raíz a swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Endpoint de salud del microservicio
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "fh.api.customer",
    timestamp = DateTime.UtcNow
}))
.WithTags("Health")
.WithSummary("Verifica el estado de salud del microservicio");

// Rutas de los controladores (Controllers/)
app.MapControllers();

app.Run();


