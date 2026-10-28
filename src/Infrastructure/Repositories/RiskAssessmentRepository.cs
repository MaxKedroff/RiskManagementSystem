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

    public async Task<bool> HasAssessmentWithNullResponseModeAsync(
        int riskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM assessment.assessments
                WHERE risk_id = @RiskId
                  AND is_active = TRUE
                  AND response_mode IS NULL
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { RiskId = riskId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateResponseModeAsync(
        int assessmentId,
        string responseMode,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE assessment.assessments
            SET response_mode = @ResponseMode
            WHERE assessment_id = @AssessmentId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { AssessmentId = assessmentId, ResponseMode = responseMode },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<bool> DeactivateAssessmentAsync(
        int riskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE assessment.assessments
            SET is_active = FALSE
            WHERE risk_id = @RiskId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { RiskId = riskId },
                cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<int?> GetActiveAssessmentIdAsync(
        int riskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT assessment_id
            FROM assessment.assessments
            WHERE risk_id = @RiskId
              AND is_active = TRUE
            LIMIT 1;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                sql,
                new { RiskId = riskId },
                cancellationToken: cancellationToken));
    }

    public async Task<PreviousAssessment?> GetPreviousAssessmentAsync(
        int riskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                a.likelihood_level_id AS LikelihoodLevelId,
                a.impact_level_id AS ImpactLevelId,
                a.loss_amount AS LossAmount,
                a.downtime_weeks AS DowntimeWeeks,
                a.downtime_days AS DowntimeDays,
                a.downtime_hours AS DowntimeHours,
                a.risk_score AS RiskScore,
                ll.likelihood_level_name AS LikelihoodName,
                il.impact_level_name AS ImpactName
            FROM assessment.assessments a
            LEFT JOIN assessment.likelihood_levels ll
                ON ll.likelihood_level_id = a.likelihood_level_id
            LEFT JOIN assessment.impact_levels il
                ON il.impact_level_id = a.impact_level_id
            WHERE a.risk_id = @RiskId
              AND a.is_active = FALSE
            ORDER BY a.created_at DESC
            LIMIT 1;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<PreviousAssessmentRow>(
            new CommandDefinition(
                sql,
                new { RiskId = riskId },
                cancellationToken: cancellationToken));

        if (row is null) return null;

        return new PreviousAssessment(
            row.LikelihoodLevelId,
            row.ImpactLevelId,
            row.LossAmount,
            row.DowntimeWeeks,
            row.DowntimeDays,
            row.DowntimeHours,
            row.RiskScore,
            row.LikelihoodName,
            row.ImpactName);
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

    private sealed class PreviousAssessmentRow
    {
        public int LikelihoodLevelId { get; init; }
        public int ImpactLevelId { get; init; }
        public decimal LossAmount { get; init; }
        public decimal? DowntimeWeeks { get; init; }
        public decimal? DowntimeDays { get; init; }
        public decimal? DowntimeHours { get; init; }
        public int RiskScore { get; init; }
        public string? LikelihoodName { get; init; }
        public string? ImpactName { get; init; }
    }

    public async Task<IReadOnlyList<LikelihoodLevelItem>> GetLikelihoodLevelsAsync(
        int methodId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                likelihood_level_id     AS LikelihoodLevelId,
                method_id               AS MethodId,
                likelihood_level_name   AS LikelihoodLevelName,
                likelihood_level_weight AS LikelihoodLevelWeight
            FROM assessment.likelihood_levels
            WHERE method_id = @MethodId
            ORDER BY likelihood_level_weight;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<LikelihoodLevelItem>(
            new CommandDefinition(sql, new { MethodId = methodId }, cancellationToken: cancellationToken));

        return rows.ToArray();
    }
}
