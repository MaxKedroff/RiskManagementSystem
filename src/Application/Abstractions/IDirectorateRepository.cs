
using Domain.Org;

namespace Application.Abstractions;

public sealed record DirectorateDependencies(
    int Persons,
    int Processes,
    int Responsibilities)
{
    public bool HasAny =>
        Persons > 0 || Processes > 0 || Responsibilities > 0;
}

public interface IDirectorateRepository
{
    Task<int> CreateAsync(
        Directorate directorate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Directorate>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Directorate?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> TitleExistsAsync(
        string title,
        int? exceptId = null,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Directorate directorate,
        CancellationToken cancellationToken = default);

    Task<DirectorateDependencies> GetDependenciesAsync(
        int directorateId,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default);
}
