using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FH.Customers.Infrastructure.CustomerPlans.Gateways;
using FH.Customers.Infrastructure.Identity;
using FH.Customers.Infrastructure.Messaging;
using FH.Customers.Application.Contracts.Gateways;
using FH.Customers.Application.Contracts.Messaging;

namespace FH.Customers.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra la capa Infrastructure: gateways HTTP hacia otros microservicios (Financials, con
    /// autenticación de servicio contra Identity) y el publicador de eventos (en memoria por ahora).
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        var identityBaseUrl = configuration?["Gateways:Identity:BaseUrl"] ?? "http://localhost:5113";
        var financialsBaseUrl = configuration?["Gateways:Financials:BaseUrl"] ?? "http://localhost:5114";

        // Cuenta de servicio contra Identity (svc-customers): obtiene y cachea el JWT que usan los gateways
        if (configuration is not null)
        {
            services.Configure<IdentityGatewayOptions>(configuration.GetSection(IdentityGatewayOptions.SectionName));
        }
        services.AddHttpClient<IdentityTokenClient>(client =>
        {
            client.BaseAddress = new Uri(identityBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // Gateway hacia Financials: valida external_plan_id (RN-08) contra el catálogo real de planes
        services.AddHttpClient<IFinancialsPlanGateway, FinancialsPlanGatewayHttp>(client =>
        {
            client.BaseAddress = new Uri(financialsBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Mensajería: publicador en memoria (registra en el log). Kafka se conecta cambiando esta línea.
        services.AddScoped<IEventPublisher, InMemoryEventPublisher>();

        return services;
    }
}
