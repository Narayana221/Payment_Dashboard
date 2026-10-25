using Payments.Application.Shared.Dtos;

namespace Payments.Application.Features.Dashboard.Dtos;

/// <param name="HasArrived">True once the money is in the bank; false while it is in transit.</param>
public sealed record PayoutLineDto(string Id, MoneyDto Amount, DateOnly ArrivalDate, bool HasArrived);
