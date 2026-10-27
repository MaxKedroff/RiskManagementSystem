using Application.Abstractions;
using Dapper;
using Domain.Org;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class AssetRepository : IAssetRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AssetRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        Asset asset,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO org.assets
                (asset_title, asset_inventory_num, asset_hour_cost, is_asset_active)
            VALUES
                (@Title, @InventoryNumber, @HourCost, TRUE)
            RETURNING asset_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new
            {
                asset.Title,
                asset.InventoryNumber,
                asset.HourCost
            }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Asset>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                asset_id             AS Id,
                asset_title          AS Title,
                asset_inventory_num  AS InventoryNumber,
                asset_hour_cost      AS HourCost,
                is_asset_active      AS IsActive
            FROM org.assets
            ORDER BY asset_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<AssetRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.Select(Map).ToArray();
    }

    public async Task<Asset?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                asset_id             AS Id,
                asset_title          AS Title,
                asset_inventory_num  AS InventoryNumber,
                asset_hour_cost      AS HourCost,
                is_asset_active      AS IsActive
            FROM org.assets
            WHERE asset_id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<AssetRow>(
            new CommandDefinition(sql, new { Id = id },
                cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<bool> UpdateAsync(
        AssetUpdate update,
        CancellationToken cancellationToken = default)
    {
        var setClauses = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("Id", update.Id);

        if (update.Title is not null)
        {
            setClauses.Add("asset_title = @Title");
            parameters.Add("Title", update.Title);
        }

        if (update.InventoryNumber is not null)
        {
            setClauses.Add("asset_inventory_num = @InventoryNumber");
            parameters.Add("InventoryNumber", update.InventoryNumber);
        }

        if (update.HourCost.HasValue)
        {
            setClauses.Add("asset_hour_cost = @HourCost");
            parameters.Add("HourCost", update.HourCost.Value);
        }

        if (update.IsActive.HasValue)
        {
            setClauses.Add("is_asset_active = @IsActive");
            parameters.Add("IsActive", update.IsActive.Value);
        }

        if (setClauses.Count == 0)
            return false;

        var sql = $"""
            UPDATE org.assets
            SET {string.Join(", ", setClauses)}
            WHERE asset_id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<bool> DeactivateAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE org.assets
            SET is_asset_active = FALSE
            WHERE asset_id = @AssetId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { AssetId = assetId },
                cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<AssetDependencies> GetDependenciesAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                (
                    SELECT COUNT(*)::int
                    FROM risk.risks
                    WHERE asset_id = @AssetId
                ) AS Risks;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<AssetDependencies>(
            new CommandDefinition(sql, new { AssetId = assetId },
                cancellationToken: cancellationToken));
    }

    private static Asset Map(AssetRow row) =>
        Asset.Rehydrate(
            row.Id,
            row.Title,
            row.InventoryNumber,
            row.HourCost,
            row.IsActive);

    private sealed class AssetRow
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? InventoryNumber { get; init; }
        public decimal HourCost { get; init; }
        public bool IsActive { get; init; }
    }
}