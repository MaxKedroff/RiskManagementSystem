using Application.Abstractions;
using Dapper;
using Domain.Org;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class ProcessRepository : IProcessRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProcessRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        BusinessProcess process,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO org.processes
                (directorate_id, process_title, owner_id, process_description, is_process_active)
            VALUES
                (@DirectorateId, @Title, @OwnerId, @Description, TRUE)
            RETURNING process_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new
            {
                process.DirectorateId,
                process.Title,
                process.OwnerId,
                process.Description
            }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<BusinessProcess>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                process_id            AS Id,
                directorate_id        AS DirectorateId,
                process_title         AS Title,
                owner_id              AS OwnerId,
                process_description   AS Description,
                is_process_active     AS IsActive
            FROM org.processes
            ORDER BY process_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<ProcessRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.Select(Map).ToArray();
    }

    public async Task<BusinessProcess?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                process_id            AS Id,
                directorate_id        AS DirectorateId,
                process_title         AS Title,
                owner_id              AS OwnerId,
                process_description   AS Description,
                is_process_active     AS IsActive
            FROM org.processes
            WHERE process_id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ProcessRow>(
            new CommandDefinition(sql, new { Id = id },
                cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<bool> UpdateAsync(
        ProcessUpdate update,
        CancellationToken cancellationToken = default)
    {
        var setClauses = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("Id", update.Id);

        if (update.DirectorateId.HasValue)
        {
            setClauses.Add("directorate_id = @DirectorateId");
            parameters.Add("DirectorateId", update.DirectorateId.Value);
        }

        if (update.Title is not null)
        {
            setClauses.Add("process_title = @Title");
            parameters.Add("Title", update.Title);
        }

        if (update.OwnerId.HasValue)
        {
            setClauses.Add("owner_id = @OwnerId");
            parameters.Add("OwnerId", update.OwnerId.Value);
        }

        if (update.Description is not null)
        {
            setClauses.Add("process_description = @Description");
            parameters.Add("Description", update.Description);
        }

        if (update.IsActive.HasValue)
        {
            setClauses.Add("is_process_active = @IsActive");
            parameters.Add("IsActive", update.IsActive.Value);
        }

        if (setClauses.Count == 0)
            return false;

        var sql = $"""
            UPDATE org.processes
            SET {string.Join(", ", setClauses)}
            WHERE process_id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<bool> DeactivateAsync(
        int processId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE org.processes
            SET is_process_active = FALSE
            WHERE process_id = @ProcessId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { ProcessId = processId },
                cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<ProcessDependencies> GetDependenciesAsync(
        int processId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                (
                    SELECT COUNT(*)
                    FROM risk.risk_processes
                    WHERE process_id = @ProcessId
                ) AS RiskProcesses,
                (
                    SELECT COUNT(*)
                    FROM org.responsibility
                    WHERE process_id = @ProcessId
                ) AS Responsibilities,
                (
                    SELECT COUNT(*)
                    FROM org.processes
                    WHERE owner_id = (
                        SELECT owner_id
                        FROM org.processes
                        WHERE process_id = @ProcessId
                    )
                ) AS OwnedProcesses;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<ProcessDependencies>(
            new CommandDefinition(sql, new { ProcessId = processId },
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

    public async Task<bool> UserExistsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM roles.users
                WHERE user_id = @UserId
                  AND is_user_active = TRUE
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { UserId = userId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> UserHasRoleAsync(
        int userId,
        string roleCode,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM roles.user_roles ur
                JOIN roles.roles r ON r.role_id = ur.role_id
                WHERE ur.user_id = @UserId
                  AND r.role_code = @RoleCode
                  AND r.is_role_active = TRUE
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { UserId = userId, RoleCode = roleCode },
                cancellationToken: cancellationToken));
    }

    private static BusinessProcess Map(ProcessRow row) =>
        BusinessProcess.Rehydrate(
            row.Id,
            row.DirectorateId,
            row.Title,
            row.OwnerId,
            row.Description,
            row.IsActive);

    public async Task<ProcessOwnerInfo?> GetOwnerInfoAsync(
        int processId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                p.process_id AS ProcessId,
                p.owner_id AS OwnerPersonId,
                pr.user_id AS OwnerUserId,
                p.is_process_active AS IsActive
            FROM org.processes p
            LEFT JOIN org.persons pr ON pr.person_id = p.owner_id
            WHERE p.process_id = @ProcessId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<OwnerInfoRow>(
            new CommandDefinition(
                sql,
                new { ProcessId = processId },
                cancellationToken: cancellationToken));

        if (row is null) return null;

        return new ProcessOwnerInfo(
            row.ProcessId,
            row.OwnerPersonId,
            row.OwnerUserId,
            row.IsActive);
    }

    private sealed class OwnerInfoRow
    {
        public int ProcessId { get; init; }
        public int OwnerPersonId { get; init; }
        public int? OwnerUserId { get; init; }
        public bool IsActive { get; init; }
    }

    private sealed class ProcessRow
    {
        public int Id { get; init; }
        public int DirectorateId { get; init; }
        public string Title { get; init; } = string.Empty;
        public int OwnerId { get; init; }
        public string? Description { get; init; }
        public bool IsActive { get; init; }
    }
}