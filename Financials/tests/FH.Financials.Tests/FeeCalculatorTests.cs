using FH.Financials.Domain.Enums;
using FH.Financials.Domain.Exceptions;
using FH.Financials.Domain.Services;

namespace FH.Financials.Tests;

public class FeeCalculatorTests
{
    private const decimal BaseFee = 100m;

    [Theory]
    [InlineData(0, 0.15)]   // recién nacido
    [InlineData(17, 0.15)]  // límite superior del rango menor
    [InlineData(18, 0.25)]  // límite inferior del rango adulto
    [InlineData(59, 0.25)]  // límite superior del rango adulto
    [InlineData(60, 0.40)]  // adulto mayor
    [InlineData(95, 0.40)]
    public void Humanos_recargan_por_edad_derivada(int age, decimal expectedRate)
    {
        var surcharges = FeeCalculator.Calculate(BaseFee, [new QuoteBeneficiary(SubjectType.Human, age, RelationshipType.Child)]);

        var s = Assert.Single(surcharges);
        Assert.Equal(expectedRate, s.Rate);
        Assert.Equal(decimal.Round(BaseFee * expectedRate, 2), s.Surcharge);
    }

    [Fact]
    public void Mascotas_recargan_10_porciento_fijo_independiente_de_la_edad()
    {
        var surcharges = FeeCalculator.Calculate(BaseFee, [new QuoteBeneficiary(SubjectType.Pet, 12, RelationshipType.Pet)]);

        var s = Assert.Single(surcharges);
        Assert.Equal(0.10m, s.Rate);
        Assert.Equal(10m, s.Surcharge);
    }

    [Fact]
    public void El_titular_principal_no_genera_recargo()
    {
        var surcharges = FeeCalculator.Calculate(BaseFee, [new QuoteBeneficiary(SubjectType.Human, 40, RelationshipType.Principal)]);

        var s = Assert.Single(surcharges);
        Assert.Equal(0m, s.Rate);
        Assert.Equal(0m, s.Surcharge);
    }

    [Fact]
    public void Varios_beneficiarios_acumulan_recargos()
    {
        var surcharges = FeeCalculator.Calculate(50000m,
        [
            new QuoteBeneficiary(SubjectType.Human, 40, RelationshipType.Principal), // 0
            new QuoteBeneficiary(SubjectType.Human, 8, RelationshipType.Child),     // 15% = 7500
            new QuoteBeneficiary(SubjectType.Human, 65, RelationshipType.Parent),   // 40% = 20000
            new QuoteBeneficiary(SubjectType.Pet, 3, RelationshipType.Pet)          // 10% = 5000
        ]);

        Assert.Equal(4, surcharges.Count);
        Assert.Equal(32500m, surcharges.Sum(s => s.Surcharge)); // 7500 + 20000 + 5000
    }

    [Fact]
    public void Cuota_base_no_positiva_rechaza()
    {
        Assert.Throws<BusinessRuleException>(
            () => FeeCalculator.Calculate(0m, [new QuoteBeneficiary(SubjectType.Human, 30, RelationshipType.Spouse)]));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(131)]
    public void Edad_fuera_de_rango_rechaza(int age)
    {
        Assert.Throws<BusinessRuleException>(
            () => FeeCalculator.Calculate(BaseFee, [new QuoteBeneficiary(SubjectType.Human, age, RelationshipType.Child)]));
    }
}
