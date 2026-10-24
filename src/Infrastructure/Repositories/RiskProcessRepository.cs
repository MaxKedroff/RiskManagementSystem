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
}
