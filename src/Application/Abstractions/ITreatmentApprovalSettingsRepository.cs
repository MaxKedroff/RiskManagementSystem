using Domain.Treatment;

namespace Application.Abstractions;

public interface ITreatmentApprovalSettingsRepository
{
    Task<TreatmentApprovalSettings?> GetAsync(
        CancellationToken cancellationToken = default);

    Task<int> SaveAsync(
        TreatmentApprovalSettings settings,
        CancellationToken cancellationToken = default);
}
