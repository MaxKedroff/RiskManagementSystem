using System.Threading;
using System.Threading.Tasks;

namespace Application.Abstractions
{
    public interface ITreatmentRepository
    {
        Task<bool> HasNonTerminalTreatmentAsync(int riskProcessId, CancellationToken cancellationToken = default);
        Task<bool> AllTreatmentsTerminalAsync(int riskProcessId, CancellationToken cancellationToken = default);
    }
}
