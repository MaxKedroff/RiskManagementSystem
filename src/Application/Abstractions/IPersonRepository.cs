
using Domain.Org;

namespace Application.Abstractions;

public sealed record PersonDependencies(
    int OwnedProcesses,
    int Responsibilities,
    int RiskProcesses)
{
    public bool HasAny =>
        OwnedProcesses > 0 ||
        Responsibilities > 0 ||
        RiskProcesses > 0;
}

public interface IPersonRepository
{
    Task<int> CreateAsync(
        Person person,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Person?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> UserExistsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<bool> DirectorateIsActiveAsync(
        int directorateId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Person person,
        CancellationToken cancellationToken = default);

    Task<PersonDependencies> GetDependenciesAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        int personId,
        CancellationToken cancellationToken = default);
}
