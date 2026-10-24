using Application.Abstractions;
using Dapper;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class UserPermissionRepository : IUserPermissionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserPermissionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> HasRightAsync(
        int userId,
        string rightCode,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM roles.user_roles ur
                JOIN roles.role_rights rr ON rr.role_id = ur.role_id
                JOIN roles.rights r ON r.right_id = rr.right_id
                WHERE ur.user_id = @UserId
                  AND r.right_code = @RightCode
                  AND ur.is_active = TRUE
                  AND r.is_active = TRUE
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { UserId = userId, RightCode = rightCode },
                cancellationToken: cancellationToken));
    }
}
