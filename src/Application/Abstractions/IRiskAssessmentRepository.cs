using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Assessment;

namespace Application.Abstractions
{
    public interface IRiskAssessmentRepository
    {
        Task<int> CreateAsync(RiskAssessment entity, CancellationToken cancellationToken = default);
        Task<bool> HasActiveAssessmentAsync(int riskId, CancellationToken cancellationToken = default);
        Task<ImpactRange?> GetImpactRangeForAmountAsync(decimal amount, CancellationToken cancellationToken = default);
        Task<AcceptanceThresholds?> GetAcceptanceThresholdsAsync(int methodId, CancellationToken cancellationToken = default);
        Task<decimal?> GetAssetHourCostAsync(int assetId, CancellationToken cancellationToken = default);
        Task<(int likelihoodWeight, int impactWeight)> GetAssessmentWeightsAsync(int likelihoodLevelId, int impactLevelId, CancellationToken cancellationToken = default);
        Task<bool> HasAssessmentWithNullResponseModeAsync(int riskId, CancellationToken cancellationToken = default);
        Task<bool> UpdateResponseModeAsync(int assessmentId, string responseMode, CancellationToken cancellationToken = default);
        Task<bool> DeactivateAssessmentAsync(int riskId, CancellationToken cancellationToken = default);
        Task<int?> GetActiveAssessmentIdAsync(int riskId, CancellationToken cancellationToken = default);
        Task<PreviousAssessment?> GetPreviousAssessmentAsync(int riskId, CancellationToken cancellationToken = default);
    }

    public sealed record PreviousAssessment(
        int LikelihoodLevelId,
        int ImpactLevelId,
        decimal LossAmount,
        decimal? DowntimeWeeks,
        decimal? DowntimeDays,
        decimal? DowntimeHours,
        int RiskScore,
        string? LikelihoodName,
        string? ImpactName);

    public sealed record ImpactRange(
        int ImpactLevelId,
        decimal Loss,
        decimal MaximumLoss);

    public sealed record AcceptanceThresholds(
        int OwnerMaxScore,
        int DirectorMaxScore,
        int CeoMaxScore);
}
