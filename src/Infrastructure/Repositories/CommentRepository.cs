using Application.Abstractions;
using Dapper;
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
}
