using Domain.Assessment;

namespace Application.Abstractions;

public interface IImpactLevelRepository
{
    Task<IReadOnlyList<ImpactLevel>> GetAllAsync(
        int methodId,
        CancellationToken cancellationToken = default);

    Task<bool> MethodExistsAsync(
        int methodId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        IReadOnlyList<ImpactLevel> levels,
        CancellationToken cancellationToken = default);
}
