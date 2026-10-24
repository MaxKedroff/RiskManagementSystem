using Application.Abstractions;
using Dapper;
using Domain.Risk;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class RiskProcessRepository : IRiskProcessRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RiskProcessRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        RiskProcess entity,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO risk.risk_processes
                (risk_id, risk_processed_owner_id, person_id, is_risk_process_primary)
            VALUES
                (@RiskId, @ProcessedOwnerUserId, @PersonId, @IsPrimary)
            RETURNING risk_process_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    entity.RiskId,
                    entity.ProcessedOwnerUserId,
                    entity.PersonId,
                    entity.IsPrimary
                },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> HasPrimaryAsync(
        int riskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM risk.risk_processes
                WHERE risk_id = @RiskId
                  AND is_risk_process_primary = TRUE
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { RiskId = riskId },
                cancellationToken: cancellationToken));
    }

    public async Task<int?> GetPrimaryOwnerUserIdAsync(
        int riskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT risk_processed_owner_id
            FROM risk.risk_processes
            WHERE risk_id = @RiskId
              AND is_risk_process_primary = TRUE
            LIMIT 1;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                sql,
                new { RiskId = riskId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> IsProcessActiveAsync(
        int processId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM org.processes
                WHERE process_id = @ProcessId
                  AND is_process_active = TRUE
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { ProcessId = processId },
                cancellationToken: cancellationToken));
    }

    public async Task<int[]> GetExistingProcessIdsAsync(
        int riskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT person_id
            FROM risk.risk_processes
            WHERE risk_id = @RiskId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<int>(
            new CommandDefinition(
                sql,
                new { RiskId = riskId },
                cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    public async Task<bool> DeactivateRiskProcessLinksAsync(
        int riskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.risk_processes
            SET is_risk_process_primary = FALSE
            WHERE risk_id = @RiskId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { RiskId = riskId },
                cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<int> CreateRiskProcessAsync(
        int riskId,
        int processedOwnerUserId,
        int personId,
        bool isPrimary,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO risk.risk_processes
                (risk_id, risk_processed_owner_id, person_id, is_risk_process_primary)
            VALUES
                (@RiskId, @ProcessedOwnerUserId, @PersonId, @IsPrimary)
            RETURNING risk_process_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    RiskId = riskId,
                    ProcessedOwnerUserId = processedOwnerUserId,
                    PersonId = personId,
                    IsPrimary = isPrimary
                },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateRiskProcessUnlinkedAtAsync(
        int riskProcessId,
        DateTimeOffset unlinkedAt,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.risk_processes
            SET unlinked_at = @UnlinkedAt
            WHERE risk_process_id = @RiskProcessId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { RiskProcessId = riskProcessId, UnlinkedAt = unlinkedAt },
                cancellationToken: cancellationToken));

        return affected == 1;
    }
}
