namespace Payments.Application.Shared.Dtos;

/// <summary>An amount in the currency's smallest unit (pence for GBP). Formatting is the client's job.</summary>
public sealed record MoneyDto(long AmountMinor, string Currency);
