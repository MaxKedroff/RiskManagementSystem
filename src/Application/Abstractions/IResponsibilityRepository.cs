using Domain.Org;
using static Domain.Org.Responsibility;

namespace Application.Abstractions;

public interface IResponsibilityRepository
{
    Task<int> CreateAsync(Responsibility responsibility, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Responsibility>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Responsibility?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ResponsibilityUpdate update, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int responsibilityId, CancellationToken cancellationToken = default);

    Task<bool> PersonExistsAsync(int personId, CancellationToken cancellationToken = default);
}