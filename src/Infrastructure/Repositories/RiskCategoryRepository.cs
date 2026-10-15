using Application.Abstractions;
using Dapper;
using Domain.Risk;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class RiskCategoryRepository : IRiskCategoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RiskCategoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        RiskCategory category,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO risk.categories
                (category_name, is_active)
            VALUES
                (@Name, TRUE)
            RETURNING category_id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new { Name = category.Name },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<RiskCategory>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                category_id AS Id,
                category_name AS Name,
                is_active AS IsActive
            FROM risk.categories
            ORDER BY category_id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<RiskCategoryRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.Select(Map).ToArray();
    }

    public async Task<RiskCategory?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                category_id AS Id,
                category_name AS Name,
                is_active AS IsActive
            FROM risk.categories
            WHERE category_id = @Id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<RiskCategoryRow>(
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
                FROM risk.categories
                WHERE lower(trim(category_name)) = lower(trim(@Name))
                  AND (@ExceptId IS NULL OR category_id <> @ExceptId)
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
        RiskCategory category,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.categories
            SET category_name = @Name,
                is_active = @IsActive
            WHERE category_id = @Id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    category.Id,
                    category.Name,
                    category.IsActive
                },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<bool> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.categories
            SET is_active = FALSE
            WHERE category_id = @Id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { Id = id },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<RiskCategoryDependencies> GetDependenciesAsync(
        int categoryId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                (
                    SELECT COUNT(*)
                    FROM risk.risks
                    WHERE category_id = @CategoryId
                ) AS Risks;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<RiskCategoryDependencies>(
            new CommandDefinition(
                sql,
                new { CategoryId = categoryId },
                cancellationToken: cancellationToken));
    }

    private static RiskCategory Map(RiskCategoryRow row) =>
        RiskCategory.Rehydrate(row.Id, row.Name, row.IsActive);

    private sealed class RiskCategoryRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }
}
