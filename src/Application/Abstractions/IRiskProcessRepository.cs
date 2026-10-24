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
    }
}
