using Application.Abstractions;
using Dapper;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class AuthRepository : IAuthRepository, IDisposable
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AuthRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public async Task<AuthUser?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        const string userSql = """
            SELECT
                user_id AS UserId,
                user_email AS Email,
                user_password_hash AS PasswordHash
            FROM roles.users
            WHERE lower(user_email) = lower(@Email)
              AND is_user_active = TRUE
            LIMIT 1;
            """;

        const string rolesSql = """
            SELECT r.role_code
            FROM roles.user_roles ur
            JOIN roles.roles r ON r.role_id = ur.role_id
            WHERE ur.user_id = @UserId
              AND ur.is_active = TRUE
              AND r.is_role_active = TRUE;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var user = await connection.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(
                userSql,
                new { Email = email.Trim() },
                cancellationToken: cancellationToken));

        if (user is null)
            return null;

        var roles = (await connection.QueryAsync<string>(
            new CommandDefinition(
                rolesSql,
                new { user.UserId },
                cancellationToken: cancellationToken)))
            .ToArray();

        return new AuthUser(
            user.UserId,
            user.Email,
            user.PasswordHash,
            roles);
    }

    private sealed class UserRow
    {
        public int UserId { get; init; }
        public string Email { get; init; } = "";
        public string PasswordHash { get; init; } = "";
    }
}