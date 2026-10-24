using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Treatment;

namespace Application.Abstractions
{
    public interface ITreatmentRepository
    {
        Task<int> CreateAsync(Domain.Treatment.Treatment entity, CancellationToken cancellationToken = default);
        Task<Domain.Treatment.Treatment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Domain.Treatment.Treatment>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Domain.Treatment.Treatment entity, CancellationToken cancellationToken = default);
        Task<bool> UpdateStatusAsync(int treatmentId, string status, CancellationToken cancellationToken = default);
        Task<bool> UpdateAssigneeAsync(int treatmentId, int? assigneeId, int assignedById, CancellationToken cancellationToken = default);
        Task<bool> HasActiveTreatmentAsync(int riskProcessId, CancellationToken cancellationToken = default);
        Task<bool> HasPendingApprovalAsync(int treatmentId, CancellationToken cancellationToken = default);
        Task<bool> HasConflictingTreatmentAsync(int riskId, int excludeTreatmentId, CancellationToken cancellationToken = default);
        Task<int?> GetActiveAssessmentIdAsync(int riskId, CancellationToken cancellationToken = default);
        Task<string?> GetRiskStatusAsync(int riskId, CancellationToken cancellationToken = default);
        Task<int?> GetProcessRiskIdAsync(int riskProcessId, CancellationToken cancellationToken = default);
        Task<int[]> GetActiveRiskProcessIdsAsync(int riskId, CancellationToken cancellationToken = default);
        Task<bool> HasNonTerminalTreatmentAsync(int riskProcessId, CancellationToken cancellationToken = default);
        Task<bool> AllTreatmentsTerminalAsync(int riskProcessId, CancellationToken cancellationToken = default);
    }
}
