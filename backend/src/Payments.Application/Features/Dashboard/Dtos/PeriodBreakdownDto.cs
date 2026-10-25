namespace Payments.Application.Features.Dashboard.Dtos;

/// <param name="Period">"day", "week" or "month".</param>
/// <param name="WindowStart">Exclusive.</param>
/// <param name="WindowEnd">Inclusive; equal to the response's asOf.</param>
public sealed record PeriodBreakdownDto(string Period, DateTimeOffset WindowStart, DateTimeOffset WindowEnd, SalesBreakdownDto Breakdown);
