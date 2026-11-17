using System.Text;
using FH.Financials.Application;
using FH.Financials.Persistence;
using FH.Financials.Persistence.Seeds;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Capas de Clean Architecture (cada una se registra con su propio método de extensión)
builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);

// Autenticación: valida los JWT emitidos por Identity (contrato compartido: issuer fh.api.identity,
// audiencia fh-apis, HS256, ClockSkew cero). Sin token / inválido / expirado -> 401; sin rol -> 403.
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

// Controladores. Enums como texto legible (ej: "Human", "Principal"), igual que Customers.
builder.Services.AddControllers(options =>
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Swagger con soporte de Bearer (pegar el token de POST /api/v1/auth/login de Identity)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Funeral Homes - Financials Microservice API",
        Version = "v1",
        Description = "Microservicio del catálogo de planes y cálculo de la cuota periódica (fh.api.financial)."
    });
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

// Catálogo inicial de planes (solo Development, idempotente)
if (app.Environment.IsDevelopment())
{
    await PlanSeeder.SeedAsync(app.Services);
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Funeral Homes Financials API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

// Manejo centralizado de excepciones con ProblemDetails (BusinessRule -> 422, KeyNotFound -> 404)
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var ex = feature?.Error;

        var statusCode = ex switch
        {
            FH.Financials.Domain.Exceptions.BusinessRuleException => StatusCodes.Status422UnprocessableEntity,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == StatusCodes.Status422UnprocessableEntity ? "Regla de Negocio Incumplida" : "Error Interno del Servidor",
            Detail = ex?.Message
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

app.UseAuthentication();
app.UseAuthorization();

// Redireccionar raíz a swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Endpoint de salud del microservicio (público)
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "fh.api.financial",
    timestamp = DateTime.UtcNow
}))
.WithTags("Health")
.WithSummary("Verifica el estado de salud del microservicio");

app.MapControllers();

app.Run();
