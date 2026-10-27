using Application.Abstractions;
using Dapper;
using Domain.Treatment;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class TreatmentRepository : ITreatmentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TreatmentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        Treatment entity,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO treatment.treatments
                (risk_process_id, treatment_title, treatment_description, treatment_status,
                 treatment_date, treatment_cost_plan, treatment_cost_act,
                 treatment_created_by_id, treatment_assigned_id, treatment_created, treatment_updated)
            VALUES
                (@RiskProcessId, @Title, @Description, @Status,
                 @DueAt, @PlannedCost, @ActualCost,
                 @CreatedById, @AssignedToId, @CreatedAt, @UpdatedAt)
            RETURNING treatment_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    entity.RiskProcessId,
                    entity.Title,
                    entity.Description,
                    Status = entity.Status.ToString(),
                    entity.DueAt,
                    entity.PlannedCost,
                    entity.ActualCost,
                    entity.CreatedById,
                    entity.AssignedToId,
                    entity.CreatedAt,
                    entity.UpdatedAt
                },
                cancellationToken: cancellationToken));
    }

    public async Task<Treatment?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                treatment_id AS Id,
                risk_process_id AS RiskProcessId,
                treatment_title AS Title,
                treatment_description AS Description,
                treatment_status AS Status,
                treatment_date AS DueAt,
                treatment_cost_plan AS PlannedCost,
                treatment_cost_act AS ActualCost,
                treatment_created_by_id AS CreatedById,
                treatment_assigned_id AS AssignedToId,
                treatment_created AS CreatedAt,
                treatment_updated AS UpdatedAt
            FROM treatment.treatments
            WHERE treatment_id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<TreatmentRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<Treatment>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                treatment_id AS Id,
                risk_process_id AS RiskProcessId,
                treatment_title AS Title,
                treatment_description AS Description,
                treatment_status AS Status,
                treatment_date AS DueAt,
                treatment_cost_plan AS PlannedCost,
                treatment_cost_act AS ActualCost,
                treatment_created_by_id AS CreatedById,
                treatment_assigned_id AS AssignedToId,
                treatment_created AS CreatedAt,
                treatment_updated AS UpdatedAt
            FROM treatment.treatments
            ORDER BY treatment_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<TreatmentRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.Select(Map).ToArray();
    }

    public async Task<bool> UpdateAsync(
        Treatment entity,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE treatment.treatments
            SET treatment_title = @Title,
                treatment_description = @Description,
                treatment_date = @DueAt,
                treatment_cost_plan = @PlannedCost,
                treatment_cost_act = @ActualCost,
                treatment_updated = @UpdatedAt
            WHERE treatment_id = @Id;
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
                    entity.DueAt,
                    entity.PlannedCost,
                    entity.ActualCost,
                    entity.UpdatedAt
                },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<bool> UpdateStatusAsync(
        int treatmentId,
        string status,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE treatment.treatments
            SET treatment_status = @Status,
                treatment_updated = NOW()
            WHERE treatment_id = @TreatmentId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { TreatmentId = treatmentId, Status = status },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<bool> UpdateAssigneeAsync(
        int treatmentId,
        int? assigneeId,
        int assignedById,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE treatment.treatments
            SET treatment_assigned_id = @AssigneeId,
                treatment_updated = NOW()
            WHERE treatment_id = @TreatmentId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { TreatmentId = treatmentId, AssigneeId = assigneeId },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<bool> HasActiveTreatmentAsync(
        int riskProcessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM treatment.treatments
                WHERE risk_process_id = @RiskProcessId
                  AND treatment_status NOT IN ('Completed', 'Cancelled')
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { RiskProcessId = riskProcessId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> HasPendingApprovalAsync(
        int treatmentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM treatment.treatments
                WHERE treatment_id = @TreatmentId
                  AND treatment_status = 'PendingApproval'
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { TreatmentId = treatmentId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> HasConflictingTreatmentAsync(
        int riskId,
        int excludeTreatmentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM treatment.treatments t
                JOIN risk.risk_processes rp ON rp.risk_process_id = t.risk_process_id
                WHERE rp.risk_id = @RiskId
                  AND t.treatment_id <> @ExcludeTreatmentId
                  AND t.treatment_status NOT IN ('Completed', 'Cancelled')
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { RiskId = riskId, ExcludeTreatmentId = excludeTreatmentId },
                cancellationToken: cancellationToken));
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

    public async Task<string?> GetRiskStatusAsync(
        int riskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT risk_status
            FROM risk.risks
            WHERE risk_id = @RiskId
            LIMIT 1;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(
                sql,
                new { RiskId = riskId },
                cancellationToken: cancellationToken));
    }

    public async Task<int?> GetProcessRiskIdAsync(
        int riskProcessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT risk_id
            FROM risk.risk_processes
            WHERE risk_process_id = @RiskProcessId
            LIMIT 1;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                sql,
                new { RiskProcessId = riskProcessId },
                cancellationToken: cancellationToken));
    }

    public async Task<int[]> GetActiveRiskProcessIdsAsync(
        int riskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT risk_process_id
            FROM risk.risk_processes
            WHERE risk_id = @RiskId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<int>(
            new CommandDefinition(
                sql,
                new { RiskId = riskId },
                cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    public async Task<bool> HasNonTerminalTreatmentAsync(
        int riskProcessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM treatment.treatments
                WHERE risk_process_id = @RiskProcessId
                  AND treatment_status IN ('Planned', 'InProgress', 'PendingApproval', 'AwaitingExecution', 'Approved', 'Hold')
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { RiskProcessId = riskProcessId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> AllTreatmentsTerminalAsync(
        int riskProcessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT NOT EXISTS (
                SELECT 1
                FROM treatment.treatments
                WHERE risk_process_id = @RiskProcessId
                  AND treatment_status NOT IN ('Completed', 'Cancelled')
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { RiskProcessId = riskProcessId },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<TreatmentListItem>> GetListAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                t.treatment_id            AS TreatmentId,
                rp.risk_id                AS RiskId,
                t.risk_process_id         AS RiskProcessId,
                t.treatment_title         AS TreatmentTitle,
                t.treatment_description   AS TreatmentDescription,
                t.treatment_status        AS TreatmentStatus,
                t.treatment_date          AS DueDate,
                t.treatment_cost_plan     AS TreatmentCostPlan,
                t.treatment_cost_act      AS TreatmentCostAct,
                t.treatment_created_by_id AS CreatedById,
                t.treatment_assigned_id   AS AssigneeId,
                t.treatment_created       AS CreatedAt,
                t.treatment_updated       AS UpdatedAt,
                ar.approval_request_id    AS ApprovalRequestId,
                ar.approval_required_level AS ApprovalRequiredLevel,
                ar.approval_request_status AS ApprovalRequestStatus
            FROM treatment.treatments t
            JOIN risk.risk_processes rp ON rp.risk_process_id = t.risk_process_id
            LEFT JOIN LATERAL (
                SELECT approval_request_id, approval_required_level, approval_request_status
                FROM treatment.approval_requests
                WHERE treatment_id = t.treatment_id
                ORDER BY approval_request_id DESC
                LIMIT 1
            ) ar ON TRUE
            ORDER BY t.treatment_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<TreatmentListItem>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    private static Treatment Map(TreatmentRow row) =>
        Treatment.Rehydrate(
            row.Id,
            row.RiskProcessId,
            row.Title,
            row.Description,
            MapStatus(row.Status),
            row.DueAt,
            row.PlannedCost,
            row.ActualCost,
            row.CreatedById,
            row.AssignedToId,
            row.CreatedAt,
            row.UpdatedAt);

    private static TreatmentStatus MapStatus(string status) =>
        status switch
        {
            "Planned" => TreatmentStatus.Planned,
            "InProgress" => TreatmentStatus.InProgress,
            "Completed" => TreatmentStatus.Completed,
            "Cancelled" => TreatmentStatus.Cancelled,
            "PendingApproval" => TreatmentStatus.PendingApproval,
            "Approved" => TreatmentStatus.Approved,
            "AwaitingExecution" => TreatmentStatus.AwaitingExecution,
            "Hold" => TreatmentStatus.Hold,
            _ => throw new InvalidOperationException($"Unknown treatment status: {status}")
        };

    private sealed class TreatmentRow
    {
        public int Id { get; init; }
        public int RiskProcessId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTimeOffset DueAt { get; init; }
        public decimal PlannedCost { get; init; }
        public decimal ActualCost { get; init; }
        public int CreatedById { get; init; }
        public int? AssignedToId { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
