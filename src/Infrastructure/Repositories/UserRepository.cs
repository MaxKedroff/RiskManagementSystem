using Application.Abstractions;
using Dapper;
using Domain.Roles;
using Infrastructure.Database;
using static Domain.Roles.User;

namespace Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        User user,
        IReadOnlyList<int> roleIds,
        CancellationToken cancellationToken = default)
    {
        const string insertUserSql = """
            INSERT INTO roles.users
                (user_fio, user_email, user_password_hash, is_user_active)
            VALUES
                (@FullName, @Email, @PasswordHash, TRUE)
            RETURNING user_id;
            """;

        const string insertPersonSql = """
            INSERT INTO org.persons (user_id, directorate_id, person_position, is_person_active)
            VALUES (@UserId, @DirectorateId, @PersonPosition, TRUE);
            """;

        const string insertRoleSql = """
            INSERT INTO roles.user_roles (user_id, role_id)
            VALUES (@UserId, @RoleId);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            var userId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(insertUserSql, new
                {
                    user.FullName,
                    user.Email,
                    user.PasswordHash
                }, transaction, cancellationToken: cancellationToken));

            if (user.PersonId.HasValue || user.DirectorateId.HasValue || user.ResponsibilityType is not null)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(insertPersonSql, new
                    {
                        UserId = userId,
                        DirectorateId = user.DirectorateId,
                        PersonPosition = user.ResponsibilityType
                    }, transaction, cancellationToken: cancellationToken));
            }

            foreach (var roleId in roleIds)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(insertRoleSql, new
                    {
                        UserId = userId,
                        RoleId = roleId
                    }, transaction, cancellationToken: cancellationToken));
            }

            transaction.Commit();
            return userId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                u.user_id              AS Id,
                u.user_fio             AS FullName,
                u.user_email           AS Email,
                u.user_password_hash   AS PasswordHash,
                u.is_user_active       AS IsActive,
                p.person_id            AS PersonId,
                p.directorate_id       AS DirectorateId,
                p.person_position      AS ResponsibilityType
            FROM roles.users u
            LEFT JOIN org.persons p ON p.user_id = u.user_id
            ORDER BY u.user_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<UserRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.Select(Map).ToArray();
    }

    public async Task<User?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                u.user_id              AS Id,
                u.user_fio             AS FullName,
                u.user_email           AS Email,
                u.user_password_hash   AS PasswordHash,
                u.is_user_active       AS IsActive,
                p.person_id            AS PersonId,
                p.directorate_id       AS DirectorateId,
                p.person_position      AS ResponsibilityType
            FROM roles.users u
            LEFT JOIN org.persons p ON p.user_id = u.user_id
            WHERE u.user_id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(sql, new { Id = id },
                cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                r.role_id         AS Id,
                r.role_code       AS Code,
                r.role_name       AS Name,
                r.is_role_active  AS IsActive
            FROM roles.user_roles ur
            JOIN roles.roles r ON r.role_id = ur.role_id
            WHERE ur.user_id = @UserId
            ORDER BY r.role_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<RoleRow>(
            new CommandDefinition(sql, new { UserId = userId },
                cancellationToken: cancellationToken));

        return rows.Select(MapRole).ToArray();
    }

    public async Task<bool> UpdateAsync(
        UserUpdate update,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            var setClauses = new List<string>();
            var parameters = new DynamicParameters();
            parameters.Add("Id", update.Id);

            if (update.FullName is not null)
            {
                setClauses.Add("user_fio = @FullName");
                parameters.Add("FullName", update.FullName);
            }

            if (update.IsActive.HasValue)
            {
                setClauses.Add("is_user_active = @IsActive");
                parameters.Add("IsActive", update.IsActive.Value);
            }

            var affected = 0;

            if (setClauses.Count > 0)
            {
                var sql = $"""
                    UPDATE roles.users
                    SET {string.Join(", ", setClauses)}
                    WHERE user_id = @Id;
                    """;

                affected = await connection.ExecuteAsync(
                    new CommandDefinition(sql, parameters, transaction,
                        cancellationToken: cancellationToken));
            }

            if (update.RoleIds is not null)
            {
                const string deleteRolesSql = """
                    DELETE FROM roles.user_roles WHERE user_id = @UserId;
                    """;

                const string insertRoleSql = """
                    INSERT INTO roles.user_roles (user_id, role_id)
                    VALUES (@UserId, @RoleId);
                    """;

                await connection.ExecuteAsync(
                    new CommandDefinition(deleteRolesSql, new { UserId = update.Id },
                        transaction, cancellationToken: cancellationToken));

                foreach (var roleId in update.RoleIds)
                {
                    await connection.ExecuteAsync(
                        new CommandDefinition(insertRoleSql, new
                        {
                            UserId = update.Id,
                            RoleId = roleId
                        }, transaction, cancellationToken: cancellationToken));
                }

                affected = 1;
            }

            transaction.Commit();
            return affected > 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdatePasswordHashAsync(
        int userId,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE roles.users
            SET user_password_hash = @PasswordHash
            WHERE user_id = @UserId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId, PasswordHash = passwordHash },
                cancellationToken: cancellationToken));

        return affected > 0;
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
                  AND is_role_active = TRUE
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { RoleId = roleId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> IsLastAdminAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT NOT EXISTS (
                SELECT 1
                FROM roles.user_roles ur
                JOIN roles.roles r ON r.role_id = ur.role_id
                JOIN roles.users u ON u.user_id = ur.user_id
                WHERE r.role_code = 'ADMIN'
                  AND u.is_user_active = TRUE
                  AND u.user_id <> @UserId
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { UserId = userId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> PersonExistsAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM org.persons
                WHERE person_id = @PersonId
                  AND is_person_active = TRUE
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { PersonId = personId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> DirectorateIsActiveAsync(
        int directorateId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM org.directorates
                WHERE directorate_id = @DirectorateId
                  AND is_directorate_active = TRUE
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { DirectorateId = directorateId },
                cancellationToken: cancellationToken));
    }

    private static User Map(UserRow row) =>
        User.Rehydrate(
            row.Id,
            row.FullName,
            row.Email,
            row.PasswordHash,
            row.IsActive,
            row.PersonId,
            row.DirectorateId,
            row.ResponsibilityType);

    private static Role MapRole(RoleRow row) =>
        Role.Rehydrate(row.Id, row.Code, row.Name, row.IsActive);

    private sealed class UserRow
    {
        public int Id { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string PasswordHash { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public int? PersonId { get; init; }
        public int? DirectorateId { get; init; }
        public string? ResponsibilityType { get; init; }
    }

    private sealed class RoleRow
    {
        public int Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }
}