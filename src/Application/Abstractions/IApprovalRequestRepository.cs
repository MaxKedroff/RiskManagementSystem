using System.Threading;
using System.Threading.Tasks;
using Domain.Treatment;

namespace Application.Abstractions
{
    public interface IApprovalRequestRepository
    {
        Task<int> CreateAsync(ApprovalRequest entity, CancellationToken cancellationToken = default);
        Task<ApprovalRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> HasOpenApprovalAsync(int treatmentId, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(ApprovalRequest entity, CancellationToken cancellationToken = default);
    }
}
