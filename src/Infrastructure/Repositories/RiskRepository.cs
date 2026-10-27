using Application.Abstractions;
using Dapper;
using Domain.Risk;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class RiskRepository : IRiskRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RiskRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        Risk entity,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO risk.risks
                (risk_title, risk_description, hazard_id, category_id, risk_author_id, asset_id, risk_status)
            VALUES
                (@Title, @Description, @HazardId, @CategoryId, @AuthorId, @AssetId, @Status)
            RETURNING risk_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    entity.Title,
                    entity.Description,
                    entity.HazardId,
                    entity.CategoryId,
                    entity.AuthorId,
                    AssetId = entity.AssetId,
                    Status = entity.Status.ToString()
                },
                cancellationToken: cancellationToken));
    }

    public async Task<Risk?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                risk_id AS Id,
                risk_title AS Title,
                risk_description AS Description,
                hazard_id AS HazardId,
                category_id AS CategoryId,
                risk_author_id AS AuthorId,
                asset_id AS AssetId,
                risk_status AS Status,
                risk_acceptance_level AS AcceptanceLevel,
                risk_close_reason AS CloseReason
            FROM risk.risks
            WHERE risk_id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<RiskRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<bool> UpdateAsync(
        Risk entity,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.risks
            SET risk_title = @Title,
                risk_description = @Description,
                hazard_id = @HazardId,
                category_id = @CategoryId,
                asset_id = @AssetId
            WHERE risk_id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    entity.Id,
                    entity.Title,
                    entity.Description,
                    entity.HazardId,
                    entity.CategoryId,
                    entity.AssetId
                },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<bool> UpdateStatusAsync(
        int riskId,
        string status,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.risks
            SET risk_status = @Status
            WHERE risk_id = @RiskId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { RiskId = riskId, Status = status },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<bool> UpdateRiskStatusAndReasonAsync(
        int riskId,
        string status,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.risks
            SET risk_status = @Status,
                risk_close_reason = @Reason
            WHERE risk_id = @RiskId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { RiskId = riskId, Status = status, Reason = reason },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<bool> SetAcceptanceLevelAsync(
        int riskId,
        string acceptanceLevel,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.risks
            SET risk_acceptance_level = @AcceptanceLevel
            WHERE risk_id = @RiskId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { RiskId = riskId, AcceptanceLevel = acceptanceLevel },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<bool> ClearAcceptanceLevelAsync(
        int riskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.risks
            SET risk_acceptance_level = NULL
            WHERE risk_id = @RiskId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { RiskId = riskId },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<bool> SetAwaitingReassessmentStatusAsync(
        int riskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.risks
            SET risk_status = 'AwaitingReassessment'
            WHERE risk_id = @RiskId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { RiskId = riskId },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<RiskListItem[]> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                r.risk_id AS RiskId,
                r.risk_title AS RiskTitle,
                r.risk_status AS RiskStatus,
                r.risk_acceptance_level AS AcceptanceLevel,
                a.asset_title AS AssetTitle,
                h.hazard_name AS HazardName,
                c.category_name AS CategoryName,
                al.likelihood_level_name AS LikelihoodName,
                il.impact_level_name AS ImpactName,
                a2.risk_score AS RiskScore
            FROM risk.risks r
            LEFT JOIN org.assets a ON r.asset_id = a.asset_id
            LEFT JOIN risk.hazards h ON r.hazard_id = h.hazard_id
            LEFT JOIN risk.categories c ON r.category_id = c.category_id
            LEFT JOIN assessment.assessments a2 ON a2.risk_id = r.risk_id AND a2.is_active = TRUE
            LEFT JOIN assessment.likelihood_levels al ON al.likelihood_level_id = a2.likelihood_level_id
            LEFT JOIN assessment.impact_levels il ON il.impact_level_id = a2.impact_level_id
            ORDER BY r.risk_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<RiskListRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        // Group by risk_id and aggregate process/directorate/owner info
        var riskGroups = rows.GroupBy(r => r.RiskId);
        var results = new List<RiskListItem>();

        foreach (var group in riskGroups)
        {
            var first = group.First();
            var riskId = group.Key;

            // Get process info for this risk
            var processInfo = await GetProcessInfoForRiskAsync(riskId, cancellationToken);

            results.Add(new RiskListItem(
                first.RiskId,
                first.RiskTitle,
                processInfo.Processes,
                processInfo.Directorates,
                processInfo.Owners,
                first.AssetTitle,
                first.HazardName,
                first.CategoryName,
                first.LikelihoodName,
                first.ImpactName,
                first.RiskScore,
                first.RiskStatus,
                first.AcceptanceLevel,
                processInfo.RiskProcessIds));
        }

        return results.ToArray();
    }

    public async Task<bool> IsActiveDraftAsync(
        int riskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM risk.risks
                WHERE risk_id = @RiskId
                  AND risk_status = 'Draft'
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { RiskId = riskId },
                cancellationToken: cancellationToken));
    }

    private async Task<(string[] Processes, string[] Directorates, string[] Owners, int[] RiskProcessIds)> GetProcessInfoForRiskAsync(
        int riskId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                rp.risk_process_id  AS RiskProcessId,
                p.process_title     AS ProcessTitle,
                d.directorate_title AS DirectorateTitle,
                u.user_fio          AS UserFio
            FROM risk.risk_processes rp
            JOIN org.processes p ON p.process_id = rp.process_id
            JOIN org.directorates d ON d.directorate_id = p.directorate_id
            JOIN org.persons pr ON pr.person_id = rp.person_id
            JOIN roles.users u ON u.user_id = rp.risk_processed_owner_id
            WHERE rp.risk_id = @RiskId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<ProcessInfoRow>(
            new CommandDefinition(sql, new { RiskId = riskId }, cancellationToken: cancellationToken));

        var processes = rows.Select(r => r.ProcessTitle).ToArray();
        var directorates = rows.Select(r => r.DirectorateTitle).ToArray();
        var owners = rows.Select(r => r.UserFio).ToArray();
        var riskProcessIds = rows.Select(r => r.RiskProcessId).ToArray();

        return (processes, directorates, owners, riskProcessIds);
    }

    private static Risk Map(RiskRow row) =>
        Risk.Rehydrate(
            row.Id,
            row.Title,
            row.Description,
            row.HazardId,
            row.CategoryId,
            row.AuthorId,
            row.AssetId,
            row.Status,
            row.AcceptanceLevel,
            row.CloseReason);

    private static RiskStatus MapStatus(string status) =>
        status switch
        {
            "Draft" => RiskStatus.Draft,
            "Open" => RiskStatus.Open,
            "InTreatment" => RiskStatus.InTreatment,
            "Accepted" => RiskStatus.Accepted,
            "Closed" => RiskStatus.Closed,
            _ => throw new InvalidOperationException($"Unknown risk status: {status}")
        };

    private static RiskAcceptanceLevel? MapAcceptanceLevel(string level) =>
        level switch
        {
            "Owner" => RiskAcceptanceLevel.Owner,
            "Director" => RiskAcceptanceLevel.Director,
            "Ceo" => RiskAcceptanceLevel.Ceo,
            _ => null
        };

    private sealed class RiskRow
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public int HazardId { get; init; }
        public int CategoryId { get; init; }
        public int AuthorId { get; init; }
        public int? AssetId { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? AcceptanceLevel { get; init; }
        public string? CloseReason { get; init; }
    }

    private sealed class RiskListRow
    {
        public int RiskId { get; init; }
        public string RiskTitle { get; init; } = string.Empty;
        public string RiskStatus { get; init; } = string.Empty;
        public string? AcceptanceLevel { get; init; }
        public string? AssetTitle { get; init; }
        public string? HazardName { get; init; }
        public string? CategoryName { get; init; }
        public string? LikelihoodName { get; init; }
        public string? ImpactName { get; init; }
        public int? RiskScore { get; init; }
    }

    private sealed class ProcessInfoRow
    {
        public int RiskProcessId { get; init; }
        public string ProcessTitle { get; init; } = string.Empty;
        public string DirectorateTitle { get; init; } = string.Empty;
        public string UserFio { get; init; } = string.Empty;
    }
}
