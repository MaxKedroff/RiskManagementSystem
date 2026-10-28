using Application.Abstractions;
using Dapper;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class UserPermissionRepository : IUserPermissionRepository
{
    // Rights come only from active roles of an active user; ADMIN has every right (03 §6.1–6.2).
    private const string ActiveRolesSql = """
        FROM roles.users u
        JOIN roles.user_roles ur ON ur.user_id = u.user_id AND ur.is_active = TRUE
        JOIN roles.roles ro ON ro.role_id = ur.role_id AND ro.is_role_active = TRUE
        WHERE u.user_id = @UserId
          AND u.is_user_active = TRUE
        """;

    // Current owner of a process: the user of its owner person card.
    private const string ProcessOwnerSql = """
        SELECT pp.user_id
        FROM org.processes p
        JOIN org.persons pp ON pp.person_id = p.owner_id
        WHERE p.process_id = rp.process_id
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public UserPermissionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task<bool> HasRightAsync(
        int userId,
        string rightCode,
        CancellationToken cancellationToken = default) =>
        ExistsAsync($"""
            SELECT EXISTS (
                SELECT 1
                {ActiveRolesSql}
                  AND (ro.role_code = 'ADMIN' OR EXISTS (
                      SELECT 1
                      FROM roles.role_rights rr
                      JOIN roles.rights r ON r.right_id = rr.right_id
                      WHERE rr.role_id = ro.role_id
                        AND r.right_code = @RightCode))
            );
            """, new { UserId = userId, RightCode = rightCode }, cancellationToken);

    public Task<bool> IsAdminAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        HasRoleAsync(userId, "ADMIN", cancellationToken);

    public Task<bool> HasRoleAsync(
        int userId,
        string roleCode,
        CancellationToken cancellationToken = default) =>
        ExistsAsync($"""
            SELECT EXISTS (
                SELECT 1
                {ActiveRolesSql}
                  AND ro.role_code = @RoleCode
            );
            """, new { UserId = userId, RoleCode = roleCode }, cancellationToken);

    public Task<bool> IsRiskOwnerAsync(
        int userId,
        int riskId,
        CancellationToken cancellationToken = default) =>
        ExistsAsync($"""
            SELECT EXISTS (
                SELECT 1
                FROM risk.risk_processes rp
                WHERE rp.risk_id = @RiskId
                  AND rp.is_risk_process_primary = TRUE
                  AND rp.unlinked_at IS NULL
                  AND COALESCE(({ProcessOwnerSql}), rp.risk_processed_owner_id) = @UserId
            );
            """, new { UserId = userId, RiskId = riskId }, cancellationToken);

    public Task<bool> IsTreatmentOwnerAsync(
        int userId,
        int treatmentId,
        CancellationToken cancellationToken = default) =>
        ExistsAsync($"""
            SELECT EXISTS (
                SELECT 1
                FROM treatment.treatments t
                JOIN risk.risk_processes rp ON rp.risk_process_id = t.risk_process_id
                WHERE t.treatment_id = @TreatmentId
                  AND COALESCE(({ProcessOwnerSql}), rp.risk_processed_owner_id) = @UserId
            );
            """, new { UserId = userId, TreatmentId = treatmentId }, cancellationToken);

    // ponytail: DIRECTOR is any DIRECTOR with an active card in the process directorate, CEO is any
    // CEO; responsibilities only narrow RISK_SPECIALIST. Use org.responsibility for all levels once
    // the administrators fill it in.
    public Task<bool> CanActAtLevelAsync(
        int userId,
        string level,
        int processId,
        CancellationToken cancellationToken = default) =>
        ExistsAsync($"""
            WITH proc AS (
                SELECT p.process_id, p.directorate_id, pp.user_id AS owner_user_id
                FROM org.processes p
                LEFT JOIN org.persons pp ON pp.person_id = p.owner_id
                WHERE p.process_id = @ProcessId
            ),
            roles_of AS (
                SELECT ro.role_code
                {ActiveRolesSql}
            ),
            cards AS (
                SELECT pe.person_id, pe.directorate_id
                FROM org.persons pe
                WHERE pe.user_id = @UserId AND pe.is_person_active = TRUE
            )
            SELECT CASE @Level
                WHEN 'Owner' THEN EXISTS (SELECT 1 FROM proc WHERE owner_user_id = @UserId)
                WHEN 'Director' THEN
                    EXISTS (SELECT 1 FROM roles_of WHERE role_code = 'DIRECTOR')
                    AND EXISTS (SELECT 1 FROM cards c JOIN proc ON proc.directorate_id = c.directorate_id)
                WHEN 'Ceo' THEN
                    EXISTS (SELECT 1 FROM roles_of WHERE role_code = 'CEO')
                    OR (EXISTS (SELECT 1 FROM roles_of WHERE role_code = 'RISK_SPECIALIST')
                        AND EXISTS (
                            SELECT 1
                            FROM cards c
                            JOIN org.responsibility rs ON rs.person_id = c.person_id
                                AND rs.responsibility_type = 'RISK_SPECIALIST'
                                AND rs.is_responsibility_active = TRUE
                            LEFT JOIN org.persons dp ON dp.person_id = rs.directed_id
                            JOIN proc ON proc.directorate_id IN (c.directorate_id, dp.directorate_id)))
                ELSE FALSE
            END;
            """, new { UserId = userId, Level = level, ProcessId = processId }, cancellationToken);

    // 12 §5.1: an active card of an active user with an active OWNER or EMPLOYEE role,
    // in the directorate of the treatment's process.
    public Task<bool> IsValidExecutorAsync(
        int assigneeUserId,
        int treatmentId,
        CancellationToken cancellationToken = default) =>
        ExistsAsync($"""
            SELECT EXISTS (
                SELECT 1
                {ActiveRolesSql.Replace("@UserId", "@AssigneeId")}
                  AND ro.role_code IN ('OWNER', 'EMPLOYEE')
                  AND EXISTS (
                      SELECT 1
                      FROM treatment.treatments t
                      JOIN risk.risk_processes rp ON rp.risk_process_id = t.risk_process_id
                      JOIN org.processes p ON p.process_id = rp.process_id
                      JOIN org.persons pe ON pe.directorate_id = p.directorate_id
                      WHERE t.treatment_id = @TreatmentId
                        AND pe.user_id = @AssigneeId
                        AND pe.is_person_active = TRUE)
            );
            """, new { AssigneeId = assigneeUserId, TreatmentId = treatmentId }, cancellationToken);

    private async Task<bool> ExistsAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }
}
