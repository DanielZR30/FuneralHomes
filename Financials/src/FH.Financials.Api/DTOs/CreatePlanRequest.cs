namespace FH.Financials.Api.DTOs;

/// <summary>Datos para crear un plan (solo Admin).</summary>
public record CreatePlanRequest(string Name, string? Description, decimal BaseMonthlyFee, int MaxBeneficiaries);
