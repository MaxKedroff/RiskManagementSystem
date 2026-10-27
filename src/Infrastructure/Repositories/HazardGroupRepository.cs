using Application.Abstractions;
using Dapper;
using Domain.Risk;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class HazardGroupRepository : IHazardGroupRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public HazardGroupRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        HazardGroup hazardGroup,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO risk.hazard_groups
                (hazard_group_name, is_active)
            VALUES
                (@Name, TRUE)
            RETURNING hazard_group_id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new { Name = hazardGroup.Name },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<HazardGroup>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                hazard_group_id AS Id,
                hazard_group_name AS Name,
                is_active AS IsActive
            FROM risk.hazard_groups
            ORDER BY hazard_group_id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<HazardGroupRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.Select(Map).ToArray();
    }

    public async Task<HazardGroup?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                hazard_group_id AS Id,
                hazard_group_name AS Name,
                is_active AS IsActive
            FROM risk.hazard_groups
            WHERE hazard_group_id = @Id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<HazardGroupRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<bool> NameExistsAsync(
        string name,
        int? exceptId = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM risk.hazard_groups
                WHERE lower(trim(hazard_group_name)) = lower(trim(@Name))
                  AND (@ExceptId IS NULL OR hazard_group_id <> @ExceptId)
            );
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { Name = name, ExceptId = exceptId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(
        HazardGroup hazardGroup,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.hazard_groups
            SET hazard_group_name = @Name,
                is_active = @IsActive
            WHERE hazard_group_id = @Id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    hazardGroup.Id,
                    hazardGroup.Name,
                    hazardGroup.IsActive
                },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<bool> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.hazard_groups
            SET is_active = FALSE
            WHERE hazard_group_id = @Id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { Id = id },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<HazardGroupDependencies> GetDependenciesAsync(
        int hazardGroupId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                (
                    SELECT COUNT(*)::int
                    FROM risk.hazards
                    WHERE hazard_group_id = @HazardGroupId
                      AND is_active = TRUE
                ) AS Hazards;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<HazardGroupDependencies>(
            new CommandDefinition(
                sql,
                new { HazardGroupId = hazardGroupId },
                cancellationToken: cancellationToken));
    }

    private static HazardGroup Map(HazardGroupRow row) =>
        HazardGroup.Rehydrate(row.Id, row.Name, row.IsActive);

    private sealed class HazardGroupRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }
}
