using Microsoft.AspNetCore.Mvc;
using Payments.Api.Shared.Validation;
using Payments.Application.Features.Dashboard;
using Payments.Application.Features.Dashboard.Dtos;

namespace Payments.Api.Controllers;

[ApiController]
[Route("api/v1/organisers/{organiserId:maxlength(64)}/dashboard")]
public sealed class DashboardController(IDashboardService dashboard) : ControllerBase
{
    /// <summary>Sales, fees, refunds, payouts and future availability for an organiser, as of an instant.</summary>
    /// <param name="organiserId">
    /// Taken from the route for this demo. In production it would come from the authenticated principal, never from the client.
    /// </param>
    /// <param name="asOf">ISO 8601 instant with a UTC offset, e.g. 2026-06-21T00:00:00Z. Defaults to now.</param>
    [HttpGet]
    [ProducesResponseType<DashboardResponseDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<DashboardResponseDto>> Get(
        string organiserId, [FromQuery] string? asOf, CancellationToken cancellationToken)
    {
        if (!AsOfParameter.TryParse(asOf, out var instant, out var error))
        {
            ModelState.AddModelError(nameof(asOf), error);
            return ValidationProblem(ModelState);
        }

        var dashboardResult = await dashboard.GetSummaryAsync(organiserId, instant, cancellationToken);
        return dashboardResult is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Organiser not found.")
            : Ok(dashboardResult);
    }
}
