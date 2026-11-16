using FH.Financials.Api.DTOs;
using FH.Financials.Application.UseCases.Plans.Common;
using FH.Financials.Application.UseCases.Plans.CreatePlan;
using FH.Financials.Application.UseCases.Plans.GetPlanById;
using FH.Financials.Application.UseCases.Plans.GetPlans;
using FH.Financials.Application.UseCases.Plans.QuotePlanFee;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FH.Financials.Api.Controllers;

/// <summary>
/// Catálogo de planes y cotización de cuotas. Todos los endpoints exigen un JWT emitido por
/// Identity (fh.api.identity); la creación de planes además exige el rol Admin (403 si no lo tiene).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/plans")]
[Tags("Plans")]
public class PlansController(
    GetPlansQueryHandler getPlans,
    GetPlanByIdQueryHandler getPlanById,
    CreatePlanCommandHandler createPlan,
    QuotePlanFeeQueryHandler quotePlanFee) : ControllerBase
{
    /// <summary>Lista el catálogo (por defecto solo planes activos).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await getPlans.HandleAsync(new GetPlansQuery(includeInactive), cancellationToken);
        return result.IsFailure ? StatusCode(result.StatusCode, new { message = result.ErrorMessage }) : Ok(result.Value);
    }

    /// <summary>Detalle de un plan (lo usa Customers para validar el external_plan_id).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getPlanById.HandleAsync(new GetPlanByIdQuery(id), cancellationToken);
        return result.IsFailure ? StatusCode(result.StatusCode, new { message = result.ErrorMessage }) : Ok(result.Value);
    }

    /// <summary>Crea un plan en el catálogo. Requiere rol Admin (HTTP 403 para Operator/Auditor).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createPlan.HandleAsync(
            new CreatePlanCommand(request.Name, request.Description, request.BaseMonthlyFee, request.MaxBeneficiaries),
            cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
        }

        return Created($"/api/v1/plans/{result.Value!.Id}", result.Value);
    }

    /// <summary>Cotiza la cuota mensual del plan con recargos por beneficiario (FeeCalculator del dominio).</summary>
    [HttpPost("{id:guid}/quote")]
    [ProducesResponseType(typeof(QuoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Quote(
        Guid id,
        [FromBody] QuoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await quotePlanFee.HandleAsync(
            new QuotePlanFeeQuery(id, request.Beneficiaries),
            cancellationToken);

        return result.IsFailure ? StatusCode(result.StatusCode, new { message = result.ErrorMessage }) : Ok(result.Value);
    }
}
