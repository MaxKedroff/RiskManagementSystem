using Application.Abstractions;
using Dapper;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class RiskEventRepository : IRiskEventRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RiskEventRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(
        int riskId,
        string kind,
        string text,
        int? authorId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO risk.risk_events (risk_id, event_kind, event_text, event_author_id)
            VALUES (@RiskId, @Kind, @Text, @AuthorId);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { RiskId = riskId, Kind = kind, Text = text, AuthorId = authorId },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<RiskEventItem>> GetAllAsync(
        int? riskId,
        CancellationToken cancellationToken = default)
    {
        // The author's active person card gives the position shown next to the name.
        const string sql = """
            SELECT
                e.event_id AS EventId,
                e.risk_id AS RiskId,
                e.event_kind AS Kind,
                e.event_text AS Text,
                e.event_author_id AS AuthorId,
                u.user_fio AS AuthorName,
                (SELECT p.person_position FROM org.persons p
                  WHERE p.user_id = e.event_author_id AND p.is_person_active
                  ORDER BY p.person_id LIMIT 1) AS AuthorPosition,
                e.event_created AS CreatedAt
            FROM risk.risk_events e
            LEFT JOIN roles.users u ON u.user_id = e.event_author_id
            WHERE CAST(@RiskId AS integer) IS NULL OR e.risk_id = CAST(@RiskId AS integer)
            ORDER BY e.event_created DESC, e.event_id DESC;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<RiskEventRow>(
            new CommandDefinition(sql, new { RiskId = riskId }, cancellationToken: cancellationToken));

        return rows
            .Select(r => new RiskEventItem(
                r.EventId, r.RiskId, r.Kind, r.Text, r.AuthorId, r.AuthorName, r.AuthorPosition, r.CreatedAt))
            .ToArray();
    }

    private sealed class RiskEventRow
    {
        public int EventId { get; init; }
        public int RiskId { get; init; }
        public string Kind { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
        public int? AuthorId { get; init; }
        public string? AuthorName { get; init; }
        public string? AuthorPosition { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
