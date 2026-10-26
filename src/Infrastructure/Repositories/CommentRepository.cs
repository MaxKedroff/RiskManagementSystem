using Application.Abstractions;
using Dapper;
using Domain.Risk;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class CommentRepository : ICommentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CommentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        int authorId,
        string text,
        string entityType,
        int entityId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO risk.comments
                (comment_author_id, comment_text, comment_entity_type, comment_entity_id, comment_created, comment_updated)
            VALUES
                (@AuthorId, @Text, @EntityType, @EntityId, NOW(), NOW())
            RETURNING comment_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    AuthorId = authorId,
                    Text = text,
                    EntityType = entityType,
                    EntityId = entityId
                },
                cancellationToken: cancellationToken));
    }

    public async Task<RiskComment?> GetByIdAsync(
        int commentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                comment_id AS Id,
                comment_author_id AS AuthorId,
                comment_text AS Text,
                comment_entity_type AS EntityType,
                comment_entity_id AS EntityId,
                is_comment_active AS IsActive,
                comment_created AS CreatedAt,
                comment_updated AS UpdatedAt
            FROM risk.comments
            WHERE comment_id = @CommentId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<CommentRow>(
            new CommandDefinition(
                sql,
                new { CommentId = commentId },
                cancellationToken: cancellationToken));

        if (row is null) return null;

        return RiskComment.Rehydrate(
            row.Id,
            row.AuthorId,
            row.Text,
            row.EntityType,
            row.EntityId,
            row.IsActive,
            row.CreatedAt,
            row.UpdatedAt);
    }

    public async Task<IReadOnlyList<RiskComment>> GetAllByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                comment_id AS Id,
                comment_author_id AS AuthorId,
                comment_text AS Text,
                comment_entity_type AS EntityType,
                comment_entity_id AS EntityId,
                is_comment_active AS IsActive,
                comment_created AS CreatedAt,
                comment_updated AS UpdatedAt
            FROM risk.comments
            WHERE comment_entity_type = @EntityType
              AND comment_entity_id = @EntityId
              AND is_comment_active = TRUE
            ORDER BY comment_created ASC;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<CommentRow>(
            new CommandDefinition(
                sql,
                new { EntityType = entityType, EntityId = entityId },
                cancellationToken: cancellationToken));

        var comments = new List<RiskComment>();
        foreach (var row in rows)
        {
            comments.Add(RiskComment.Rehydrate(
                row.Id,
                row.AuthorId,
                row.Text,
                row.EntityType,
                row.EntityId,
                row.IsActive,
                row.CreatedAt,
                row.UpdatedAt));
        }

        return comments;
    }

    public async Task<bool> EditAsync(
        int commentId,
        string text,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.comments
            SET comment_text = @Text,
                comment_updated = NOW()
            WHERE comment_id = @CommentId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { CommentId = commentId, Text = text },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<bool> SoftDeleteAsync(
        int commentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE risk.comments
            SET is_comment_active = FALSE,
                comment_updated = NOW()
            WHERE comment_id = @CommentId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { CommentId = commentId },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    private sealed class CommentRow
    {
        public int Id { get; init; }
        public int AuthorId { get; init; }
        public string Text { get; init; } = string.Empty;
        public string EntityType { get; init; } = string.Empty;
        public int EntityId { get; init; }
        public bool IsActive { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
