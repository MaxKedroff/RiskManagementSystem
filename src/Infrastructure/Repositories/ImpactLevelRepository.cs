using Application.Abstractions;
using Dapper;
using Domain.Assessment;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class ImpactLevelRepository : IImpactLevelRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ImpactLevelRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ImpactLevel>> GetAllAsync(
        int methodId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                impact_level_id AS Id,
                method_id AS MethodId,
                impact_level_name AS Name,
                impact_level_weight AS Weight,
                impact_level_loss AS Loss,
                impact_level_max AS MaximumLoss
            FROM assessment.impact_levels
            WHERE method_id = @MethodId
            ORDER BY impact_level_loss;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<ImpactLevelRow>(
            new CommandDefinition(sql, new { MethodId = methodId }, cancellationToken: cancellationToken));

        return rows.Select(Map).ToArray();
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

    public async Task SaveAsync(
        IReadOnlyList<ImpactLevel> levels,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO assessment.impact_levels
                (method_id, impact_level_name, impact_level_weight, impact_level_loss, impact_level_max)
            VALUES
                (@MethodId, @Name, @Weight, @Loss, @Max)
            ON CONFLICT DO NOTHING;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        foreach (var level in levels)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        level.MethodId,
                        level.Name,
                        level.Weight,
                        level.Loss,
                        Max = level.MaximumLoss
                    },
                    cancellationToken: cancellationToken));
        }
    }

    private static ImpactLevel Map(ImpactLevelRow row) =>
        ImpactLevel.Rehydrate(row.Id, row.MethodId, row.Name, row.Weight, row.Loss, row.MaximumLoss);

    private sealed class ImpactLevelRow
    {
        public int Id { get; init; }
        public int MethodId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Weight { get; init; }
        public decimal Loss { get; init; }
        public decimal MaximumLoss { get; init; }
    }
}
