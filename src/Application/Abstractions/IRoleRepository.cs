using Domain.Roles;

namespace Application.Abstractions;

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Right>> GetRightsAsync(int roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Right>> GetAllRightsAsync(CancellationToken cancellationToken = default);

    Task<bool> RoleExistsAsync(int roleId, CancellationToken cancellationToken = default);

    Task<bool> RightExistsAsync(int rightId, CancellationToken cancellationToken = default);

    Task<bool> IsAdminRoleAsync(int roleId, CancellationToken cancellationToken = default);

    Task<bool> RoleHasRightAsync(int roleId, string rightCode, CancellationToken cancellationToken = default);

    Task<bool> UpdateRightsAsync(int roleId, IReadOnlyList<int> rightIds, CancellationToken cancellationToken = default);
}