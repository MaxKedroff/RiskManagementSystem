using Domain.Assessment;

namespace Application.Abstractions;

public interface IRiskAcceptanceSettingsRepository
{
    Task<RiskAcceptanceSettings?> GetByMethodIdAsync(
        int methodId,
        CancellationToken cancellationToken = default);

    Task<bool> MethodExistsAsync(
        int methodId,
        CancellationToken cancellationToken = default);

    Task<int> SaveAsync(
        RiskAcceptanceSettings settings,
        CancellationToken cancellationToken = default);
}
