using System.Text;
using Identity.App.Data;
using Identity.App.Seeds;
using Identity.App.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Persistencia: PostgreSQL (fh_db_identity) con las tablas de ASP.NET Core Identity
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("IdentityDb")
        ?? "Host=localhost;Port=5432;Database=fh_db_identity;Username=postgres;Password=secret"));

// ASP.NET Core Identity: usuarios y roles con EF Core. Sin confirmación de correo ni lockout
// (los usuarios iniciales se crean con el seeder; no hay CRUD en esta entrega).
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<IdentityDbContext>();

// Autenticación: este servicio también valida sus propios JWT (endpoint /me y pruebas manuales).
// Contrato compartido con Customers y Financials: issuer fh.api.identity, audiencia fh-apis, HS256,
// ClockSkew cero para que un token expirado falle en el segundo exacto.
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
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddControllers();

// Swagger / OpenAPI con soporte de Bearer para pegar el token del login y probar /me
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Funeral Homes - Identity Microservice API",
        Version = "v1",
        Description = "Microservicio de autenticación y emisión de JWT (fh.api.identity)."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega aquí el accessToken devuelto por POST /api/v1/auth/login (sin la palabra Bearer)."
    });
    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            // Estilo Microsoft.OpenApi 2.x: referencia por id al esquema "Bearer" definido arriba
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

var app = builder.Build();

// Usuarios y roles iniciales (solo Development, idempotente)
if (app.Environment.IsDevelopment())
{
    await IdentitySeeder.SeedAsync(app.Services);
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Funeral Homes Identity API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Redireccionar raíz a swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Endpoint de salud del microservicio (público)
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "fh.api.identity",
    timestamp = DateTime.UtcNow
}))
.WithTags("Health")
.WithSummary("Verifica el estado de salud del microservicio");

app.MapControllers();

app.Run();
