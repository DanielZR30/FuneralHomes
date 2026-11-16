using FH.Financials.Application.UseCases.Plans.CreatePlan;
using FH.Financials.Application.UseCases.Plans.GetPlanById;
using FH.Financials.Application.UseCases.Plans.GetPlans;
using FH.Financials.Application.UseCases.Plans.QuotePlanFee;
using Microsoft.Extensions.DependencyInjection;

namespace FH.Financials.Application;

public static class DependencyInjection
{
    /// <summary>Registra la capa Application: casos de uso del catálogo de planes y la cotización de cuotas.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetPlansQueryHandler>();
        services.AddScoped<GetPlanByIdQueryHandler>();
        services.AddScoped<CreatePlanCommandHandler>();
        services.AddScoped<QuotePlanFeeQueryHandler>();
        return services;
    }
}
