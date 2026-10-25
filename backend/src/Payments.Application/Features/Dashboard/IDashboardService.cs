using Payments.Application.Features.Dashboard.Dtos;

namespace Payments.Application.Features.Dashboard;

public interface IDashboardService
{
    /// <param name="asOf">The instant to report at; <c>null</c> means now.</param>
    /// <returns>The dashboard, or <c>null</c> if the organiser is unknown.</returns>
    Task<DashboardResponseDto?> GetSummaryAsync(string organiserId, DateTimeOffset? asOf, CancellationToken cancellationToken);
}
