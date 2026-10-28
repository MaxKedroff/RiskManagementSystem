using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Risk;

namespace Application.Abstractions
{
    public interface IRiskProcessRepository
    {
        Task<int> CreateAsync(RiskProcess entity, CancellationToken cancellationToken = default);
        Task<bool> HasPrimaryAsync(int riskId, CancellationToken cancellationToken = default);
        Task<int?> GetPrimaryOwnerUserIdAsync(int riskId, CancellationToken cancellationToken = default);
        Task<bool> IsProcessActiveAsync(int processId, CancellationToken cancellationToken = default);
        Task<int[]> GetExistingProcessIdsAsync(int riskId, CancellationToken cancellationToken = default);
        Task<int?> GetPrimaryProcessIdAsync(int riskId, CancellationToken cancellationToken = default);
        Task<bool> HasOpenTreatmentsOnProcessAsync(int riskId, int processId, CancellationToken cancellationToken = default);
        Task UnlinkProcessAsync(int riskId, int processId, CancellationToken cancellationToken = default);
        Task SetPrimaryAsync(int riskId, int processId, CancellationToken cancellationToken = default);
        Task<bool> DeactivateRiskProcessLinksAsync(int riskId, CancellationToken cancellationToken = default);
        Task<int> CreateRiskProcessAsync(int riskId, int processId, int processedOwnerUserId, int personId, bool isPrimary, CancellationToken cancellationToken = default);
    }
}
