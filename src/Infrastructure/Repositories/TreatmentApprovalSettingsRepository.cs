using Application.Abstractions;
using Dapper;
using Domain.Treatment;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class TreatmentApprovalSettingsRepository : ITreatmentApprovalSettingsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TreatmentApprovalSettingsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TreatmentApprovalSettings?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                treatment_approval_set_id AS Id,
                owner_threshold AS OwnerThreshold,
                director_threshold AS DirectorThreshold,
                ceo_threshold AS CeoThreshold
            FROM treatment.treatment_approval_settings
            ORDER BY treatment_approval_set_id DESC
            LIMIT 1;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<Row>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<int> SaveAsync(
        TreatmentApprovalSettings settings,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH updated AS (
                UPDATE treatment.treatment_approval_settings
                SET owner_threshold = @Owner,
                    director_threshold = @Director,
                    ceo_threshold = @Ceo
                RETURNING treatment_approval_set_id
            ), inserted AS (
                INSERT INTO treatment.treatment_approval_settings
                    (owner_threshold, director_threshold, ceo_threshold)
                SELECT @Owner, @Director, @Ceo
                WHERE NOT EXISTS (SELECT 1 FROM updated)
                RETURNING treatment_approval_set_id
            )
            SELECT treatment_approval_set_id FROM updated
            UNION ALL
            SELECT treatment_approval_set_id FROM inserted
            LIMIT 1;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    Owner = settings.OwnerThreshold,
                    Director = settings.DirectorThreshold,
                    Ceo = settings.CeoThreshold
                },
                cancellationToken: cancellationToken));
    }

    private static TreatmentApprovalSettings Map(Row row) =>
        TreatmentApprovalSettings.Rehydrate(
            row.Id, row.OwnerThreshold, row.DirectorThreshold, row.CeoThreshold);

    private sealed class Row
    {
        public int Id { get; init; }
        public int OwnerThreshold { get; init; }
        public int DirectorThreshold { get; init; }
        public int CeoThreshold { get; init; }
    }
}
