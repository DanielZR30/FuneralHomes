using FH.Financials.Domain.Entities;
using FH.Financials.Domain.Enums;
using FH.Financials.Domain.Exceptions;

namespace FH.Financials.Tests;

public class PlanEntityTests
{
    [Fact]
    public void Create_crea_plan_activo_con_id()
    {
        var plan = Plan.Create("Plan Prueba", "descripción", 1000m, 5);

        Assert.NotEqual(Guid.Empty, plan.Id);
        Assert.Equal(PlanStatus.Active, plan.Status);
        Assert.Equal(1000m, plan.BaseMonthlyFee);
        Assert.Equal(5, plan.MaxBeneficiaries);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_sin_nombre_rechaza(string name)
    {
        Assert.Throws<BusinessRuleException>(() => Plan.Create(name, null, 1000m, 5));
    }

    [Fact]
    public void Create_con_nombre_muy_largo_rechaza()
    {
        Assert.Throws<BusinessRuleException>(() => Plan.Create(new string('x', Plan.NameMaxLength + 1), null, 1000m, 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_con_cuota_no_positiva_rechaza(decimal fee)
    {
        Assert.Throws<BusinessRuleException>(() => Plan.Create("Plan", null, fee, 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Create_con_cupo_fuera_de_rango_rechaza(int max)
    {
        Assert.Throws<BusinessRuleException>(() => Plan.Create("Plan", null, 1000m, max));
    }

    [Fact]
    public void CreateSeeded_respeta_el_id_fijo()
    {
        var id = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var plan = Plan.CreateSeeded(id, "Plan Familiar Esencial", null, 50000m, 4);

        Assert.Equal(id, plan.Id);
    }
}
