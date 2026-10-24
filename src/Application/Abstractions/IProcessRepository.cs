using Domain.Org;

namespace Application.Abstractions;

public interface IProcessRepository
{
    Task<int> CreateAsync(BusinessProcess process, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BusinessProcess>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<BusinessProcess?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ProcessUpdate update, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int processId, CancellationToken cancellationToken = default);

    Task<ProcessDependencies> GetDependenciesAsync(int processId, CancellationToken cancellationToken = default);

    Task<bool> DirectorateIsActiveAsync(int directorateId, CancellationToken cancellationToken = default);

    Task<bool> UserExistsAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> UserHasRoleAsync(int userId, string roleCode, CancellationToken cancellationToken = default);

    Task<ProcessOwnerInfo?> GetOwnerInfoAsync(int processId, CancellationToken cancellationToken = default);
}

public sealed record ProcessOwnerInfo(
    int ProcessId,
    int OwnerPersonId,
    int? OwnerUserId,
    bool IsActive);