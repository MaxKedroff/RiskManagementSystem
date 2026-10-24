using Application.Abstractions;
using Dapper;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class TreatmentRepository : ITreatmentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TreatmentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> HasNonTerminalTreatmentAsync(
        int riskProcessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM treatment.treatments
                WHERE risk_process_id = @RiskProcessId
                  AND treatment_status IN ('Planned', 'InProgress')
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { RiskProcessId = riskProcessId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> AllTreatmentsTerminalAsync(
        int riskProcessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT NOT EXISTS (
                SELECT 1
                FROM treatment.treatments
                WHERE risk_process_id = @RiskProcessId
                  AND treatment_status NOT IN ('Completed', 'Cancelled')
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { RiskProcessId = riskProcessId },
                cancellationToken: cancellationToken));
    }
}
