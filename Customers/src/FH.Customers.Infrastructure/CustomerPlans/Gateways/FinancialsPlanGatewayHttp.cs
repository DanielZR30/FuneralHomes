using System.Net;
using System.Net.Http.Headers;
using FH.Customers.Application.Contracts.Gateways;
using FH.Customers.Infrastructure.Identity;
using Microsoft.Extensions.Logging;

namespace FH.Customers.Infrastructure.CustomerPlans.Gateways;

/// <summary>
/// Gateway HTTP hacia el microservicio Financials (fh.api.financial). Valida el external_plan_id
/// consultando GET /api/v1/plans/{id}, autenticándose con el JWT de la cuenta de servicio que
/// emite Identity (los endpoints de Financials están protegidos). 200 = existe; 404 = no existe.
/// </summary>
public class FinancialsPlanGatewayHttp(
    HttpClient httpClient,
    IdentityTokenClient identityTokens,
    ILogger<FinancialsPlanGatewayHttp> logger) : IFinancialsPlanGateway
{
    public async Task<bool> PlanExistsAsync(Guid externalPlanId, CancellationToken cancellationToken = default)
    {
        var accessToken = await identityTokens.GetAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/plans/{externalPlanId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            return true;
        }
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        // 401/403 (token de servicio vencido o mal configurado) o 5xx: falla ruidosa, nada de
        // suponer que el plan existe
        logger.LogError("Financials respondió {Status} al validar el plan {PlanId}",
            (int)response.StatusCode, externalPlanId);
        throw new InvalidOperationException(
            $"No se pudo validar el plan {externalPlanId} contra Financials ({(int)response.StatusCode}).");
    }
}
