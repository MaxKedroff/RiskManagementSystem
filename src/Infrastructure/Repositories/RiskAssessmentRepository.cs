using Application.Abstractions;
using Dapper;
using Domain.Assessment;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class RiskAssessmentRepository : IRiskAssessmentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RiskAssessmentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        RiskAssessment entity,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO assessment.assessments
                (risk_id, method_id, likelihood_level_id, impact_level_id,
                 loss_amount, downtime_weeks, downtime_days, downtime_hours,
                 downtime_total_hours, hourly_downtime_cost, risk_score,
                 response_mode, is_active, created_by_id, created_at)
            VALUES
                (@RiskId, @MethodId, @LikelihoodLevelId, @ImpactLevelId,
                 @LossAmount, @DowntimeWeeks, @DowntimeDays, @DowntimeHours,
                 @TotalDowntimeHours, @HourlyDowntimeCost, @RiskScore,
                 @ResponseMode, @IsActive, @CreatedById, @CreatedAt)
            RETURNING assessment_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    entity.RiskId,
                    entity.MethodId,
                    entity.LikelihoodLevelId,
                    entity.ImpactLevelId,
                    entity.LossAmount,
                    DowntimeWeeks = entity.DowntimeWeeks,
                    DowntimeDays = entity.DowntimeDays,
                    DowntimeHours = entity.DowntimeHours,
                    TotalDowntimeHours = entity.TotalDowntimeHours,
                    HourlyDowntimeCost = entity.HourlyDowntimeCost,
                    entity.RiskScore,
                    ResponseMode = entity.ResponseMode,
                    entity.IsActive,
                    entity.CreatedById,
                    entity.CreatedAt
                },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> HasActiveAssessmentAsync(
        int riskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM assessment.assessments
                WHERE risk_id = @RiskId
                  AND is_active = TRUE
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { RiskId = riskId },
                cancellationToken: cancellationToken));
    }

    public async Task<ImpactRange?> GetImpactRangeForAmountAsync(
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                impact_level_id AS ImpactLevelId,
                impact_level_loss AS Loss,
                impact_level_max AS MaximumLoss
            FROM assessment.impact_levels
            WHERE @Amount >= impact_level_loss
              AND @Amount <= impact_level_max
            ORDER BY impact_level_weight DESC
            LIMIT 1;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ImpactRangeRow>(
            new CommandDefinition(
                sql,
                new { Amount = amount },
                cancellationToken: cancellationToken));

        if (row is null) return null;

        return new ImpactRange(row.ImpactLevelId, row.Loss, row.MaximumLoss);
    }

    public async Task<AcceptanceThresholds?> GetAcceptanceThresholdsAsync(
        int methodId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                owner_max_score AS OwnerMaxScore,
                director_max_score AS DirectorMaxScore,
                ceo_max_score AS CeoMaxScore
            FROM assessment.risk_acceptance_settings
            WHERE method_id = @MethodId
            LIMIT 1;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ThresholdsRow>(
            new CommandDefinition(
                sql,
                new { MethodId = methodId },
                cancellationToken: cancellationToken));

        if (row is null) return null;

        return new AcceptanceThresholds(row.OwnerMaxScore, row.DirectorMaxScore, row.CeoMaxScore);
    }

    public async Task<decimal?> GetAssetHourCostAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT asset_hour_cost
            FROM org.assets
            WHERE asset_id = @AssetId
              AND is_asset_active = TRUE
            LIMIT 1;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<decimal?>(
            new CommandDefinition(
                sql,
                new { AssetId = assetId },
                cancellationToken: cancellationToken));
    }

    public async Task<(int likelihoodWeight, int impactWeight)> GetAssessmentWeightsAsync(
        int likelihoodLevelId,
        int impactLevelId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                (SELECT likelihood_level_weight FROM assessment.likelihood_levels
                 WHERE likelihood_level_id = @LikelihoodLevelId) AS LikelihoodWeight,
                (SELECT impact_level_weight FROM assessment.impact_levels
                 WHERE impact_level_id = @ImpactLevelId) AS ImpactWeight;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleAsync<WeightsRow>(
            new CommandDefinition(
                sql,
                new { LikelihoodLevelId = likelihoodLevelId, ImpactLevelId = impactLevelId },
                cancellationToken: cancellationToken));

        return (row.LikelihoodWeight, row.ImpactWeight);
    }

    private sealed class ImpactRangeRow
    {
        public int ImpactLevelId { get; init; }
        public decimal Loss { get; init; }
        public decimal MaximumLoss { get; init; }
    }

    private sealed class ThresholdsRow
    {
        public int OwnerMaxScore { get; init; }
        public int DirectorMaxScore { get; init; }
        public int CeoMaxScore { get; init; }
    }

    private sealed class WeightsRow
    {
        public int LikelihoodWeight { get; init; }
        public int ImpactWeight { get; init; }
    }
}
