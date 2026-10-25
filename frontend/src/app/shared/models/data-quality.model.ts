/**
 * Something the organiser should know about the source data.
 * Mirrors `DataQualityIssueDto` in backend/src/Payments.Application/Shared/Dtos.
 */
export interface DataQualityIssue {
  /** camelCase issue type, e.g. "duplicateRecord". */
  readonly type: string;
  readonly message: string;
  readonly recordIds: readonly string[];
  /** True when the records were left out of every figure; false when kept with a warning. */
  readonly excludedFromTotals: boolean;
}
