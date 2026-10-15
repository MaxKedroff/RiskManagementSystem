using Domain.Risk;

namespace Application.Abstractions;

public interface IHazardGroupRepository
{
    Task<int> CreateAsync(
        HazardGroup hazardGroup,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HazardGroup>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<HazardGroup?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string name,
        int? exceptId = null,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        HazardGroup hazardGroup,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<HazardGroupDependencies> GetDependenciesAsync(
        int hazardGroupId,
        CancellationToken cancellationToken = default);
}
