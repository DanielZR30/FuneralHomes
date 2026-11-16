using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FH.Customers.Infrastructure.Identity;

/// <summary>Configuración del gateway hacia Identity (sección Gateways:Identity del appsettings).</summary>
public class IdentityGatewayOptions
{
    public const string SectionName = "Gateways:Identity";

    public string BaseUrl { get; set; } = "http://localhost:5113";
    public string ServiceEmail { get; set; } = "svc-customers@funeralhomes.local";
    public string ServicePassword { get; set; } = string.Empty;
}

/// <summary>
/// Cliente de servicio contra Identity: obtiene un JWT con la cuenta técnica svc-customers y lo
/// cachea en memoria hasta 2 minutos antes de su expiración. Lo usan los gateways HTTP para
/// llamar endpoints protegidos de otros microservicios (autenticación servicio a servicio).
/// </summary>
public class IdentityTokenClient(
    HttpClient httpClient,
    IOptions<IdentityGatewayOptions> options,
    ILogger<IdentityTokenClient> logger)
{
    private sealed record LoginRequest(string Email, string Password);
    private sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _accessToken;
    private DateTime _expiresAtUtc = DateTime.MinValue;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Válido y con margen de 2 minutos antes de expirar: se usa el cacheado
        if (_accessToken is not null && DateTime.UtcNow < _expiresAtUtc.AddMinutes(-2))
        {
            return _accessToken;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is not null && DateTime.UtcNow < _expiresAtUtc.AddMinutes(-2))
            {
                return _accessToken; // otro hilo lo renovó mientras esperábamos
            }

            var credentials = new LoginRequest(options.Value.ServiceEmail, options.Value.ServicePassword);
            var response = await httpClient.PostAsJsonAsync("api/v1/auth/login", credentials, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Login de servicio contra Identity falló con {Status}", (int)response.StatusCode);
                throw new InvalidOperationException(
                    $"No se pudo autenticar la cuenta de servicio contra Identity ({(int)response.StatusCode}).");
            }

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Respuesta vacía del login de Identity.");

            _accessToken = login.AccessToken;
            _expiresAtUtc = login.ExpiresAtUtc;
            logger.LogInformation("Token de servicio renovado (expira {ExpiresAtUtc:u})", _expiresAtUtc);

            return _accessToken;
        }
        finally
        {
            _lock.Release();
        }
    }
}
