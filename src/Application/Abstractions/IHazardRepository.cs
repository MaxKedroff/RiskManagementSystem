using Domain.Risk;

namespace Application.Abstractions;

public interface IHazardRepository
{
    Task<int> CreateAsync(
        Hazard hazard,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Hazard>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Hazard?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string name,
        int hazardGroupId,
        int? exceptId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HazardGroupIsActiveAsync(
        int hazardGroupId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Hazard hazard,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<HazardDependencies> GetDependenciesAsync(
        int hazardId,
        CancellationToken cancellationToken = default);
}
