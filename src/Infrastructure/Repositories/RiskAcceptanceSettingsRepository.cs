using Application.Abstractions;
using Dapper;
using Domain.Assessment;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class RiskAcceptanceSettingsRepository : IRiskAcceptanceSettingsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RiskAcceptanceSettingsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RiskAcceptanceSettings?> GetByMethodIdAsync(
        int methodId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                setting_id AS Id,
                method_id AS MethodId,
                owner_max_score AS OwnerMaxScore,
                director_max_score AS DirectorMaxScore,
                ceo_max_score AS CeoMaxScore
            FROM assessment.risk_acceptance_settings
            WHERE method_id = @MethodId;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<Row>(
            new CommandDefinition(sql, new { MethodId = methodId }, cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<bool> MethodExistsAsync(
        int methodId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM assessment.methods
                WHERE method_id = @MethodId
            );
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { MethodId = methodId },
                cancellationToken: cancellationToken));
    }

    public async Task<int> SaveAsync(
        RiskAcceptanceSettings settings,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO assessment.risk_acceptance_settings
                (method_id, owner_max_score, director_max_score, ceo_max_score)
            VALUES (@MethodId, @Owner, @Director, @Ceo)
            ON CONFLICT (method_id) DO UPDATE SET
                owner_max_score = EXCLUDED.owner_max_score,
                director_max_score = EXCLUDED.director_max_score,
                ceo_max_score = EXCLUDED.ceo_max_score
            RETURNING setting_id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    settings.MethodId,
                    Owner = settings.OwnerMaxScore,
                    Director = settings.DirectorMaxScore,
                    Ceo = settings.CeoMaxScore
                },
                cancellationToken: cancellationToken));
    }

    private static RiskAcceptanceSettings Map(Row row) =>
        RiskAcceptanceSettings.Rehydrate(
            row.Id, row.MethodId, row.OwnerMaxScore, row.DirectorMaxScore, row.CeoMaxScore);

    private sealed class Row
    {
        public int Id { get; init; }
        public int MethodId { get; init; }
        public int OwnerMaxScore { get; init; }
        public int DirectorMaxScore { get; init; }
        public int CeoMaxScore { get; init; }
    }
}
