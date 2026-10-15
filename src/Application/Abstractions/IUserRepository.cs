using Domain.Roles;
using static Domain.Roles.User;

namespace Application.Abstractions;

public interface IUserRepository
{
    Task<int> CreateAsync(User user, IReadOnlyList<int> roleIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> GetRolesAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(UserUpdate update, CancellationToken cancellationToken = default);

    Task<bool> UpdatePasswordHashAsync(int userId, string passwordHash, CancellationToken cancellationToken = default);

    Task<bool> RoleExistsAsync(int roleId, CancellationToken cancellationToken = default);

    Task<bool> IsLastAdminAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> PersonExistsAsync(int personId, CancellationToken cancellationToken = default);

    Task<bool> DirectorateIsActiveAsync(int directorateId, CancellationToken cancellationToken = default);
}