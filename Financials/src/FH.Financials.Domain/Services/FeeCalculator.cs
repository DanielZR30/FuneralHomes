using FH.Financials.Domain.Enums;
using FH.Financials.Domain.Exceptions;

namespace FH.Financials.Domain.Services;

/// <summary>Beneficiario de entrada para cotizar la cuota (edad derivada calculada por Customers).</summary>
public sealed record QuoteBeneficiary(SubjectType SubjectType, int DerivedAge, RelationshipType RelationshipType);

/// <summary>Recargo calculado para un beneficiario.</summary>
public sealed record QuoteSurcharge(
    SubjectType SubjectType,
    int DerivedAge,
    RelationshipType RelationshipType,
    decimal Rate,
    decimal Surcharge);

/// <summary>
/// Servicio de dominio que calcula la cuota mensual de un plan a partir de la cuota base y los
/// beneficiarios cubiertos: el titular (Principal) no genera recargo; los humanos pagan según la
/// edad derivada (0–17: 15%, 18–59: 25%, 60+: 40%) y las mascotas un 10% fijo.
/// </summary>
public static class FeeCalculator
{
    public const decimal MinorRate = 0.15m;
    public const decimal AdultRate = 0.25m;
    public const decimal SeniorRate = 0.40m;
    public const decimal PetRate = 0.10m;

    public const int MaxAge = 130;

    /// <summary>Devuelve el desglose de recargos por beneficiario (la cuota total la arma el caso de uso).</summary>
    /// <exception cref="BusinessRuleException">Cuota base no positiva o edad derivada fuera de rango.</exception>
    public static IReadOnlyList<QuoteSurcharge> Calculate(decimal baseMonthlyFee, IEnumerable<QuoteBeneficiary> beneficiaries)
    {
        if (baseMonthlyFee <= 0)
        {
            throw new BusinessRuleException("La cuota base mensual debe ser mayor que cero.");
        }

        var result = new List<QuoteSurcharge>();
        foreach (var beneficiary in beneficiaries)
        {
            if (beneficiary.DerivedAge < 0 || beneficiary.DerivedAge > MaxAge)
            {
                throw new BusinessRuleException($"La edad derivada debe estar entre 0 y {MaxAge} años (recibida: {beneficiary.DerivedAge}).");
            }

            var rate = RateFor(beneficiary);
            var surcharge = decimal.Round(baseMonthlyFee * rate, 2, MidpointRounding.AwayFromZero);
            result.Add(new QuoteSurcharge(beneficiary.SubjectType, beneficiary.DerivedAge, beneficiary.RelationshipType, rate, surcharge));
        }

        return result;
    }

    public static decimal RateFor(QuoteBeneficiary beneficiary) =>
        beneficiary.RelationshipType == RelationshipType.Principal ? 0m
        : beneficiary.SubjectType == SubjectType.Pet ? PetRate
        : beneficiary.DerivedAge < 18 ? MinorRate
        : beneficiary.DerivedAge < 60 ? AdultRate
        : SeniorRate;
}
