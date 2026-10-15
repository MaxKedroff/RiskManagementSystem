using Application.Abstractions;
using Dapper;
using Domain.Roles;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private const string AdminRoleCode = "ADMIN";

    private readonly IDbConnectionFactory _connectionFactory;

    public RoleRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                role_id          AS Id,
                role_code        AS Code,
                role_name        AS Name,
                is_role_active   AS IsActive
            FROM roles.roles
            ORDER BY role_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<RoleRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.Select(MapRole).ToArray();
    }

    public async Task<Role?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                role_id          AS Id,
                role_code        AS Code,
                role_name        AS Name,
                is_role_active   AS IsActive
            FROM roles.roles
            WHERE role_id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<RoleRow>(
            new CommandDefinition(sql, new { Id = id },
                cancellationToken: cancellationToken));

        return row is null ? null : MapRole(row);
    }

    public async Task<IReadOnlyList<Right>> GetRightsAsync(
        int roleId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                r.right_id           AS Id,
                r.right_code         AS Code,
                r.right_description  AS Description
            FROM roles.role_rights rr
            JOIN roles.rights r ON r.right_id = rr.right_id
            WHERE rr.role_id = @RoleId
            ORDER BY r.right_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<RightRow>(
            new CommandDefinition(sql, new { RoleId = roleId },
                cancellationToken: cancellationToken));

        return rows.Select(MapRight).ToArray();
    }

    public async Task<IReadOnlyList<Right>> GetAllRightsAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                right_id           AS Id,
                right_code         AS Code,
                right_description  AS Description
            FROM roles.rights
            ORDER BY right_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<RightRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.Select(MapRight).ToArray();
    }

    public async Task<bool> RoleExistsAsync(
        int roleId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM roles.roles
                WHERE role_id = @RoleId
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { RoleId = roleId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> RightExistsAsync(
        int rightId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM roles.rights
                WHERE right_id = @RightId
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { RightId = rightId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> IsAdminRoleAsync(
        int roleId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM roles.roles
                WHERE role_id = @RoleId
                  AND UPPER(role_code) = @AdminCode
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { RoleId = roleId, AdminCode = AdminRoleCode },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> RoleHasRightAsync(
        int roleId,
        string rightCode,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM roles.role_rights rr
                JOIN roles.rights r ON r.right_id = rr.right_id
                WHERE rr.role_id = @RoleId
                  AND UPPER(r.right_code) = @RightCode
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new
            {
                RoleId = roleId,
                RightCode = rightCode.ToUpperInvariant()
            }, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateRightsAsync(
        int roleId,
        IReadOnlyList<int> rightIds,
        CancellationToken cancellationToken = default)
    {
        const string deleteSql = """
            DELETE FROM roles.role_rights WHERE role_id = @RoleId;
            """;

        const string insertSql = """
            INSERT INTO roles.role_rights (role_id, right_id)
            VALUES (@RoleId, @RightId);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(deleteSql, new { RoleId = roleId },
                    transaction, cancellationToken: cancellationToken));

            foreach (var rightId in rightIds)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(insertSql, new
                    {
                        RoleId = roleId,
                        RightId = rightId
                    }, transaction, cancellationToken: cancellationToken));
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static Role MapRole(RoleRow row) =>
        Role.Rehydrate(row.Id, row.Code, row.Name, row.IsActive);

    private static Right MapRight(RightRow row) =>
        Right.Rehydrate(row.Id, row.Code, row.Description);

    private sealed class RoleRow
    {
        public int Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }

    private sealed class RightRow
    {
        public int Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }
}