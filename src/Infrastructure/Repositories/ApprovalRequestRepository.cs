using Application.Abstractions;
using Dapper;
using Domain.Treatment;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class ApprovalRequestRepository : IApprovalRequestRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ApprovalRequestRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        ApprovalRequest entity,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO treatment.approval_requests
                (treatment_id, approval_required_level, approval_request_status,
                 approval_created_by_id, approval_created, approval_comment)
            VALUES
                (@TreatmentId, @RequiredLevel, @Status,
                 @CreatedById, @CreatedAt, @Comment)
            RETURNING approval_request_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    entity.TreatmentId,
                    RequiredLevel = entity.RequiredLevel.ToString(),
                    Status = entity.Status.ToString(),
                    entity.CreatedById,
                    entity.CreatedAt,
                    entity.Comment
                },
                cancellationToken: cancellationToken));
    }

    public async Task<ApprovalRequest?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                approval_request_id AS Id,
                treatment_id AS TreatmentId,
                approval_required_level AS RequiredLevel,
                approval_request_status AS Status,
                approval_created_by_id AS CreatedById,
                approval_created AS CreatedAt,
                approval_comment AS Comment,
                approval_date AS DecisionAt,
                approval_reason AS Reason,
                approval_by_id AS DecidedById
            FROM treatment.approval_requests
            WHERE approval_request_id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ApprovalRequestRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<bool> HasOpenApprovalAsync(
        int treatmentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM treatment.approval_requests
                WHERE treatment_id = @TreatmentId
                  AND approval_request_status = 'Pending'
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { TreatmentId = treatmentId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(
        ApprovalRequest entity,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE treatment.approval_requests
            SET approval_request_status = @Status,
                approval_comment = @Comment,
                approval_date = @DecisionAt,
                approval_reason = @Reason,
                approval_by_id = @DecidedById
            WHERE approval_request_id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    entity.Id,
                    Status = entity.Status.ToString(),
                    entity.Comment,
                    entity.DecisionAt,
                    entity.Reason,
                    entity.DecidedById
                },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    private static ApprovalRequest Map(ApprovalRequestRow row) =>
        ApprovalRequest.Rehydrate(
            row.Id,
            row.TreatmentId,
            MapLevel(row.RequiredLevel),
            MapStatus(row.Status),
            row.CreatedById,
            row.CreatedAt,
            row.Comment,
            row.DecisionAt,
            row.Reason,
            row.DecidedById);

    private static ApprovalLevel MapLevel(string level) =>
        level switch
        {
            "Owner" => ApprovalLevel.Owner,
            "Director" => ApprovalLevel.Director,
            "Ceo" => ApprovalLevel.Ceo,
            _ => throw new InvalidOperationException($"Unknown approval level: {level}")
        };

    private static ApprovalStatus MapStatus(string status) =>
        status switch
        {
            "Pending" => ApprovalStatus.Pending,
            "Approved" => ApprovalStatus.Approved,
            "Rejected" => ApprovalStatus.Rejected,
            "Cancelled" => ApprovalStatus.Cancelled,
            _ => throw new InvalidOperationException($"Unknown approval status: {status}")
        };

    private sealed class ApprovalRequestRow
    {
        public int Id { get; init; }
        public int TreatmentId { get; init; }
        public string RequiredLevel { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public int CreatedById { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public string? Comment { get; init; }
        public DateTimeOffset? DecisionAt { get; init; }
        public string? Reason { get; init; }
        public int? DecidedById { get; init; }
    }

    public async Task<IReadOnlyList<ApprovalRequestItem>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                ar.approval_request_id     AS ApprovalRequestId,
                ar.treatment_id            AS TreatmentId,
                ar.approval_required_level AS ApprovalRequiredLevel,
                ar.approval_request_status AS ApprovalRequestStatus,
                ar.approval_created_by_id  AS CreatedById,
                cu.user_fio                AS CreatedByName,
                ar.approval_created        AS CreatedAt,
                ar.approval_comment        AS Comment,
                ar.approval_date           AS DecidedAt,
                ar.approval_by_id          AS DecidedById,
                du.user_fio                AS DecidedByName,
                ar.approval_reason         AS Reason
            FROM treatment.approval_requests ar
            LEFT JOIN roles.users cu ON cu.user_id = ar.approval_created_by_id
            LEFT JOIN roles.users du ON du.user_id = ar.approval_by_id
            ORDER BY ar.approval_request_id DESC;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<ApprovalRequestItem>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.ToArray();
    }
}
