using Application.Abstractions;
using Dapper;
using Domain.Org;
using Infrastructure.Database;
using static Domain.Org.Responsibility;

namespace Infrastructure.Repositories;

public sealed class ResponsibilityRepository : IResponsibilityRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ResponsibilityRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        Responsibility responsibility,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO org.responsibility
                (person_id, directed_id, responsibility_type, is_responsibility_active)
            VALUES
                (@PersonId, @DirectedPersonId, @Type, TRUE)
            RETURNING responsibility_type_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new
            {
                responsibility.PersonId,
                responsibility.DirectedPersonId,
                responsibility.Type
            }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Responsibility>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                responsibility_type_id     AS Id,
                person_id                  AS PersonId,
                directed_id         AS DirectedPersonId,
                responsibility_type        AS Type,
                is_responsibility_active   AS IsActive
            FROM org.responsibility
            ORDER BY responsibility_type_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<ResponsibilityRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.Select(Map).ToArray();
    }

    public async Task<Responsibility?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                responsibility_type_id     AS Id,
                person_id                  AS PersonId,
                directed_id         AS DirectedPersonId,
                responsibility_type        AS Type,
                is_responsibility_active   AS IsActive
            FROM org.responsibility
            WHERE responsibility_type_id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ResponsibilityRow>(
            new CommandDefinition(sql, new { Id = id },
                cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<bool> UpdateAsync(
        ResponsibilityUpdate update,
        CancellationToken cancellationToken = default)
    {
        var setClauses = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("Id", update.Id);

        if (update.PersonId.HasValue)
        {
            setClauses.Add("person_id = @PersonId");
            parameters.Add("PersonId", update.PersonId.Value);
        }

        if (update.DirectedPersonId.HasValue)
        {
            setClauses.Add("directed_id = @DirectedPersonId");
            parameters.Add("DirectedPersonId", update.DirectedPersonId.Value);
        }

        if (update.Type is not null)
        {
            setClauses.Add("responsibility_type = @Type");
            parameters.Add("Type", update.Type);
        }

        if (update.IsActive.HasValue)
        {
            setClauses.Add("is_responsibility_active = @IsActive");
            parameters.Add("IsActive", update.IsActive.Value);
        }

        if (setClauses.Count == 0)
            return false;

        var sql = $"""
            UPDATE org.responsibility
            SET {string.Join(", ", setClauses)}
            WHERE responsibility_type_id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<bool> DeactivateAsync(
        int responsibilityId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE org.responsibility
            SET is_responsibility_active = FALSE
            WHERE responsibility_type_id = @ResponsibilityId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { ResponsibilityId = responsibilityId },
                cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<bool> PersonExistsAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM org.persons
                WHERE person_id = @PersonId
                  AND is_person_active = TRUE
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { PersonId = personId },
                cancellationToken: cancellationToken));
    }

    private static Responsibility Map(ResponsibilityRow row) =>
        Responsibility.Rehydrate(
            row.Id,
            row.PersonId,
            row.DirectedPersonId,
            row.Type,
            row.IsActive);

    private sealed class ResponsibilityRow
    {
        public int Id { get; init; }
        public int PersonId { get; init; }
        public int? DirectedPersonId { get; init; }
        public string Type { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }
}