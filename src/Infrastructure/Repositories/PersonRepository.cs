
using Application.Abstractions;
using Dapper;
using Domain.Org;
using Infrastructure.Database;
using static Domain.Org.Person;

namespace Infrastructure.Repositories;

public sealed class PersonRepository : IPersonRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PersonRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        Person person,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO org.persons
                (user_id, directorate_id, person_position, is_person_active)
            VALUES
                (@UserId, @DirectorateId, @Position, TRUE)
            RETURNING person_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new
            {
                person.UserId,
                person.DirectorateId,
                person.Position
            }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Person>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                person_id AS Id,
                user_id AS UserId,
                directorate_id AS DirectorateId,
                person_position AS Position,
                is_person_active AS IsActive
            FROM org.persons
            ORDER BY person_id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<PersonRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.Select(Map).ToArray();
    }

    public async Task<Person?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                person_id AS Id,
                user_id AS UserId,
                directorate_id AS DirectorateId,
                person_position AS Position,
                is_person_active AS IsActive
            FROM org.persons
            WHERE person_id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<PersonRow>(
            new CommandDefinition(sql, new { Id = id },
                cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<bool> UserExistsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM roles.users
                WHERE user_id = @UserId
                  AND is_user_active = TRUE
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { UserId = userId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> DirectorateIsActiveAsync(
        int directorateId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM org.directorates
                WHERE directorate_id = @DirectorateId
                  AND is_directorate_active = TRUE
            );
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { DirectorateId = directorateId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(
    PersonUpdate update,
    CancellationToken cancellationToken = default)
    {
        var setClauses = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("Id", update.Id);

        if (update.DirectorateId.HasValue)
        {
            setClauses.Add("directorate_id = @DirectorateId");
            parameters.Add("DirectorateId", update.DirectorateId.Value);
        }

        if (update.Position is not null)
        {
            setClauses.Add("person_position = @Position");
            parameters.Add("Position", update.Position);
        }

        if (update.IsActive.HasValue)
        {
            setClauses.Add("is_person_active = @IsActive");
            parameters.Add("IsActive", update.IsActive.Value);
        }

        if (setClauses.Count == 0)
            return false; // нечего обновлять

        var sql = $"""
        UPDATE org.persons
        SET {string.Join(", ", setClauses)}
        WHERE person_id = @Id;
        """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<PersonDependencies> GetDependenciesAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                (
                    SELECT COUNT(*)::int
                    FROM org.processes
                    WHERE owner_id = @PersonId
                ) AS OwnedProcesses,
                (
                    SELECT COUNT(*)::int
                    FROM org.responsibility
                    WHERE person_id = @PersonId
                ) AS Responsibilities,
                (
                    SELECT COUNT(*)::int
                    FROM risk.risk_processes
                    WHERE person_id = @PersonId
                ) AS RiskProcesses;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<PersonDependencies>(
            new CommandDefinition(sql, new { PersonId = personId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> DeactivateAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE org.persons
            SET is_person_active = FALSE
            WHERE person_id = @PersonId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { PersonId = personId },
                cancellationToken: cancellationToken));

        return affected > 0;
    }

    private static Person Map(PersonRow row) =>
        Person.Rehydrate(
            row.Id,
            row.UserId,
            row.DirectorateId,
            row.Position,
            row.IsActive);

    private sealed class PersonRow
    {
        public int Id { get; init; }
        public int UserId { get; init; }
        public int DirectorateId { get; init; }
        public string Position { get; init; } = "";
        public bool IsActive { get; init; }
    }
}
