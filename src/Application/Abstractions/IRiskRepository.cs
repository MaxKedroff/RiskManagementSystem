using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Abstractions
{
    public interface IRiskRepository
    {
        Task<int> CreateAsync(Domain.Risk.Risk entity, CancellationToken cancellationToken = default);
        Task<Domain.Risk.Risk?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Domain.Risk.Risk entity, CancellationToken cancellationToken = default);
        Task<bool> UpdateStatusAsync(int riskId, string status, CancellationToken cancellationToken = default);
        Task<bool> UpdateRiskStatusAndReasonAsync(int riskId, string status, string? reason, CancellationToken cancellationToken = default);
        Task<bool> SetAcceptanceLevelAsync(int riskId, string acceptanceLevel, CancellationToken cancellationToken = default);
        Task<bool> ClearAcceptanceLevelAsync(int riskId, CancellationToken cancellationToken = default);
        Task<bool> SetAwaitingReassessmentStatusAsync(int riskId, CancellationToken cancellationToken = default);
        Task<RiskListItem[]> GetAllAsync(CancellationToken cancellationToken = default);
        Task<RiskDetails?> GetDetailsAsync(int riskId, CancellationToken cancellationToken = default);
        Task<bool> IsActiveDraftAsync(int riskId, CancellationToken cancellationToken = default);
    }

    public sealed record RiskListItem(
        int RiskId,
        string RiskTitle,
        string[] Processes,
        string[] Directorates,
        string[] Owners,
        string? Asset,
        string? Hazard,
        string? Category,
        string? Likelihood,
        string? Impact,
        int? RiskScore,
        string RiskStatus,
        string? AcceptanceLevel,
        int[]? RiskProcessIds = null,
        string? ResponseMode = null);
}
