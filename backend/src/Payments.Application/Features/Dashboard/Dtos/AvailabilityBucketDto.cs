using Payments.Application.Shared.Dtos;

namespace Payments.Application.Features.Dashboard.Dtos;

/// <summary>Funds that become eligible for payout on <paramref name="AvailableOn"/>. Never a predicted payout date.</summary>
/// <param name="Amount">Can be negative (e.g. a refund of a sale that was already paid out).</param>
/// <param name="IsAvailable">True when the date has passed and the money is waiting for the next payout.</param>
public sealed record AvailabilityBucketDto(DateOnly AvailableOn, MoneyDto Amount, bool IsAvailable);
