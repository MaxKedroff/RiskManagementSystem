using Application.Abstractions;
using Domain.Roles;
using System.IO;

namespace Application.Org.Roles;

public sealed class RoleService
{
    private const string AdminRoleCode = "ADMIN";

    private readonly IRoleRepository _repository;

    public RoleService(IRoleRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<Role>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task<IReadOnlyList<Right>> GetRightsAsync(
        int roleId,
        CancellationToken cancellationToken = default)
    {
        if (roleId <= 0)
            throw new RoleValidationException("Некорректный ID роли.");

        var role = await _repository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new RoleNotFoundException(roleId);

        return await _repository.GetRightsAsync(roleId, cancellationToken);
    }

    public Task<IReadOnlyList<Right>> GetAllRightsAsync(
        CancellationToken cancellationToken = default) =>
        _repository.GetAllRightsAsync(cancellationToken);

    public async Task<int> UpdateRightsAsync(
        int roleId,
        IReadOnlyList<int> rightIds,
        CancellationToken cancellationToken = default)
    {
        if (roleId <= 0)
            throw new RoleValidationException("Некорректный ID роли.");

        if (rightIds is null)
            throw new RoleValidationException("Список прав обязателен.");

        var role = await _repository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new RoleNotFoundException(roleId);

        foreach (var rightId in rightIds)
        {
            if (!await _repository.RightExistsAsync(rightId, cancellationToken))
                throw new RightNotFoundException(rightId);
        }

        if (await _repository.IsAdminRoleAsync(roleId, cancellationToken))
        {
            var hasAdminRight = await _repository.RoleHasRightAsync(
                roleId,
                AdminRoleCode,
                cancellationToken);

            if (hasAdminRight)
            {
                var adminRightId = await GetAdminRightIdAsync(cancellationToken);
                if (adminRightId.HasValue && !rightIds.Contains(adminRightId.Value))
                {
                    throw new RoleValidationException(
                        "Нельзя снять право admin с роли ADMIN.");
                }
            }
        }

        var updated = await _repository.UpdateRightsAsync(
            roleId,
            rightIds,
            cancellationToken);

        if (!updated)
            throw new RoleNotFoundException(roleId);

        return roleId;
    }

    private async Task<int?> GetAdminRightIdAsync(CancellationToken cancellationToken)
    {
        var rights = await _repository.GetAllRightsAsync(cancellationToken);
        var adminRight = rights.FirstOrDefault(r =>
            string.Equals(r.Code, AdminRoleCode, StringComparison.OrdinalIgnoreCase));

        return adminRight?.Id;
    }
}

public sealed class RoleValidationException(string message)
    : Exception(message);

public sealed class RoleNotFoundException(int id)
    : Exception($"Роль с ID {id} не найдена.");

public sealed class RoleConflictException(string message)
    : Exception(message);

public sealed class RightNotFoundException(int id)
    : Exception($"Право с ID {id} не найдено.");