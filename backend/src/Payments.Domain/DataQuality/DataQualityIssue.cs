namespace Payments.Domain.DataQuality;

public enum DataQualityIssueType
{
    DuplicateRecord,
    ConflictingDuplicate,
    MalformedRecord,
    UnknownRecordType,
    UnknownFeeType,
    NonSucceededCharge,
    InvalidAmount,
    OrphanRefund,
    RefundBeforeCharge,
    RefundCurrencyMismatch,
    OverRefund,
    NetMismatch,
    RefundFeeMismatch,
    StripeFeeReturned,
    UnknownPayoutTransaction,
    PayoutCurrencyMismatch,
    PayoutAmountMismatch,
    TransactionInMultiplePayouts,
    PayoutCoversLaterTransaction,
}

/// <summary>
/// Something in the source data the organiser should know about.
/// <paramref name="ExcludedFromTotals"/> is true when the record(s) were left out of every figure,
/// false when they were kept and the issue is a warning about their contents.
/// </summary>
public sealed record DataQualityIssue(
    DataQualityIssueType Type,
    string Message,
    IReadOnlyList<string> RecordIds,
    bool ExcludedFromTotals);
