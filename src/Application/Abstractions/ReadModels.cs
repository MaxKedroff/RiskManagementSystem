namespace Application.Abstractions;

// Read models for screens: the risk card, scales and approval history.

public sealed class RiskDetails
{
    public int RiskId { get; init; }
    public string RiskTitle { get; init; } = string.Empty;
    public string RiskDescription { get; init; } = string.Empty;
    public string RiskStatus { get; init; } = string.Empty;
    public string? AcceptanceLevel { get; init; }
    public string? CloseReason { get; init; }
    public int HazardId { get; init; }
    public string? Hazard { get; init; }
    public int CategoryId { get; init; }
    public string? Category { get; init; }
    public int? AssetId { get; init; }
    public string? Asset { get; init; }
    public decimal? AssetHourCost { get; init; }
    public int AuthorId { get; init; }
    public string? AuthorName { get; init; }
    public string? AuthorPosition { get; init; }
    public RiskLinkItem[] Processes { get; set; } = [];
    public AssessmentItem[] Assessments { get; set; } = [];
}

public sealed class RiskLinkItem
{
    public int RiskProcessId { get; init; }
    public int? ProcessId { get; init; }
    public string? ProcessTitle { get; init; }
    public string? Directorate { get; init; }
    public int OwnerUserId { get; init; }
    public string? OwnerName { get; init; }
    public bool IsPrimary { get; init; }
    public bool IsActive { get; init; }
}

public sealed class AssessmentItem
{
    public int AssessmentId { get; init; }
    public int LikelihoodLevelId { get; init; }
    public int LikelihoodWeight { get; init; }
    public string? Likelihood { get; init; }
    public int ImpactLevelId { get; init; }
    public int ImpactWeight { get; init; }
    public string? Impact { get; init; }
    public decimal LossAmount { get; init; }
    public decimal? DowntimeWeeks { get; init; }
    public decimal? DowntimeDays { get; init; }
    public decimal? DowntimeHours { get; init; }
    public decimal? DowntimeTotalHours { get; init; }
    public decimal? HourlyDowntimeCost { get; init; }
    public int RiskScore { get; init; }
    public string? ResponseMode { get; init; }
    public bool IsActive { get; init; }
    public int CreatedById { get; init; }
    public string? CreatedByName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class LikelihoodLevelItem
{
    public int LikelihoodLevelId { get; init; }
    public int MethodId { get; init; }
    public string LikelihoodLevelName { get; init; } = string.Empty;
    public int LikelihoodLevelWeight { get; init; }
}

public sealed class ApprovalRequestItem
{
    public int ApprovalRequestId { get; init; }
    public int TreatmentId { get; init; }
    public string ApprovalRequiredLevel { get; init; } = string.Empty;
    public string ApprovalRequestStatus { get; init; } = string.Empty;
    public int CreatedById { get; init; }
    public string? CreatedByName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? Comment { get; init; }
    public DateTimeOffset? DecidedAt { get; init; }
    public int? DecidedById { get; init; }
    public string? DecidedByName { get; init; }
    public string? Reason { get; init; }
}
