using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Abstractions
{
    public interface IUserPermissionRepository
    {
        Task<bool> HasRightAsync(int userId, string rightCode, CancellationToken cancellationToken = default);
        Task<bool> IsAdminAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> HasRoleAsync(int userId, string roleCode, CancellationToken cancellationToken = default);
        Task<bool> IsRiskOwnerAsync(int userId, int riskId, CancellationToken cancellationToken = default);
        Task<bool> IsTreatmentOwnerAsync(int userId, int treatmentId, CancellationToken cancellationToken = default);
        Task<bool> CanActAtLevelAsync(int userId, string level, int processId, CancellationToken cancellationToken = default);
        Task<bool> IsValidExecutorAsync(int assigneeUserId, int treatmentId, CancellationToken cancellationToken = default);
    }
}
