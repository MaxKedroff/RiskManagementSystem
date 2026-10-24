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
    }

    public sealed record ImpactRange(
        int ImpactLevelId,
        decimal Loss,
        decimal MaximumLoss);

    public sealed record AcceptanceThresholds(
        int OwnerMaxScore,
        int DirectorMaxScore,
        int CeoMaxScore);
}
