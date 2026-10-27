using Application.Abstractions;
using Dapper;
using Domain.Risk;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class HazardRepository : IHazardRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public HazardRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        Hazard hazard,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO risk.hazards
                (hazard_group_id, hazard_name, is_active)
            VALUES
                (@HazardGroupId, @Name, TRUE)
            RETURNING hazard_id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new { HazardGroupId = hazard.HazardGroupId, Name = hazard.Name },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Hazard>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                hazard_id AS Id,
                hazard_group_id AS HazardGroupId,
                hazard_name AS Name,
                is_active AS IsActive
            FROM risk.hazards
            ORDER BY hazard_id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<HazardRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.Select(Map).ToArray();
    }

    public async Task<Hazard?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                hazard_id AS Id,
                hazard_group_id AS HazardGroupId,
                hazard_name AS Name,
                is_active AS IsActive
            FROM risk.hazards
            WHERE hazard_id = @Id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<HazardRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<bool> NameExistsAsync(
        string name,
        int hazardGroupId,
        int? exceptId = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM risk.hazards
                WHERE lower(trim(hazard_name)) = lower(trim(@Name))
                  AND hazard_group_id = @HazardGroupId
                  AND (@ExceptId IS NULL OR hazard_id <> @ExceptId)
            );
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { Name = name, HazardGroupId = hazardGroupId, ExceptId = exceptId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> HazardGroupIsActiveAsync(
        int hazardGroupId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM risk.hazard_groups
                WHERE hazard_group_id = @HazardGroupId
                  AND is_active = TRUE
            );
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { HazardGroupId = hazardGroupId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(
        Hazard hazard,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.hazards
            SET hazard_group_id = @HazardGroupId,
                hazard_name = @Name,
                is_active = @IsActive
            WHERE hazard_id = @Id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    hazard.Id,
                    hazard.HazardGroupId,
                    hazard.Name,
                    hazard.IsActive
                },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<bool> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.hazards
            SET is_active = FALSE
            WHERE hazard_id = @Id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { Id = id },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<HazardDependencies> GetDependenciesAsync(
        int hazardId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                (
                    SELECT COUNT(*)::int
                    FROM risk.risks
                    WHERE hazard_id = @HazardId
                ) AS Risks;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<HazardDependencies>(
            new CommandDefinition(
                sql,
                new { HazardId = hazardId },
                cancellationToken: cancellationToken));
    }

    private static Hazard Map(HazardRow row) =>
        Hazard.Rehydrate(row.Id, row.HazardGroupId, row.Name, row.IsActive);

    private sealed class HazardRow
    {
        public int Id { get; init; }
        public int HazardGroupId { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }
}
