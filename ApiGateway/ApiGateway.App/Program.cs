var builder = WebApplication.CreateBuilder(args);

// API Gateway (YARP): un único punto de entrada para los tres microservicios.
// Las rutas y los destinos viven en appsettings.json (sección "ReverseProxy").
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseHttpsRedirection();

// La interfaz web (wwwroot) se sirve desde el mismo origen que el gateway:
// el navegador solo habla con https://localhost:7038, así que no se necesita CORS.
app.UseDefaultFiles();
app.UseStaticFiles();

// Salud del gateway (público)
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "fh.api.gateway",
    timestamp = DateTime.UtcNow
}))
.WithTags("Health");

// /identity/**   -> Identity   (https://localhost:7036)
// /customers/**  -> Customers  (https://localhost:7035)
// /financials/** -> Financials (https://localhost:7037)
app.MapReverseProxy();

app.Run();
