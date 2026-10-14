using Application.Abstractions;
using Dapper;
using Domain.Org;
using Infrastructure.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public sealed class DirectorateRepository : IDirectorateRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public DirectorateRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CreateAsync(Directorate directorate, CancellationToken cancellationToken = default)
        {
            const string sql = """
                INSERT INTO org.directorates
                    (directorate_title, is_directorate_active)
                VALUES
                    (@Title, TRUE)
                RETURNING directorate_id;
                """;

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            return await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    sql,
                    new { directorate.Title },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
        {
            const string sql = """
                UPDATE org.directorates
                SET is_directorate_active = FALSE
                WHERE directorate_id = @Id;
                """;

            await using var connection =
                await _connectionFactory.CreateOpenConnectionAsync(
                    cancellationToken);

            var affected = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql, new { Id = id },
                    cancellationToken: cancellationToken));

            return affected == 1;
        }

        public async Task<IReadOnlyList<Directorate>> GetAllAsync(
        CancellationToken cancellationToken = default)
        {
            const string sql = """
                SELECT
                    directorate_id AS Id,
                    directorate_title AS Title,
                    is_directorate_active AS IsActive
                FROM org.directorates
                ORDER BY directorate_title, directorate_id;
                """;

            await using var connection =
                await _connectionFactory.CreateOpenConnectionAsync(
                    cancellationToken);

            var rows = await connection.QueryAsync<DirectorateRow>(
                new CommandDefinition(sql, cancellationToken: cancellationToken));

            return rows.Select(ToDomain).ToArray();
        }

        public async Task<Directorate?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
        {
            const string sql = """
                SELECT
                    directorate_id AS Id,
                    directorate_title AS Title,
                    is_directorate_active AS IsActive
                FROM org.directorates
                WHERE directorate_id = @Id;
                """;

            await using var connection =
                await _connectionFactory.CreateOpenConnectionAsync(
                    cancellationToken);

            var row = await connection.QuerySingleOrDefaultAsync<DirectorateRow>(
                new CommandDefinition(
                    sql, new { Id = id },
                    cancellationToken: cancellationToken));

            return row is null ? null : ToDomain(row);
        }

        public async Task<DirectorateDependencies> GetDependenciesAsync(
        int directorateId,
        CancellationToken cancellationToken = default)
        {
            const string sql = """
                SELECT
                    (SELECT COUNT(*)
                     FROM org.persons p
                     WHERE p.directorate_id = @Id) AS Persons,

                    (SELECT COUNT(*)
                     FROM org.processes pr
                     WHERE pr.directorate_id = @Id) AS Processes,

                    (SELECT COUNT(*)
                     FROM org.responsibility r
                     JOIN org.persons p ON p.person_id = r.person_id
                     WHERE p.directorate_id = @Id) AS Responsibilities;
                """;

            await using var connection =
                await _connectionFactory.CreateOpenConnectionAsync(
                    cancellationToken);

            return await connection.QuerySingleAsync<DirectorateDependencies>(
                new CommandDefinition(
                    sql, new { Id = directorateId },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> TitleExistsAsync(
        string title,
        int? exceptId = null,
        CancellationToken cancellationToken = default)
        {
            const string sql = """
                SELECT EXISTS (
                    SELECT 1
                    FROM org.directorates
                    WHERE lower(trim(directorate_title)) = lower(trim(@Title))
                      AND (@ExceptId IS NULL OR directorate_id <> @ExceptId)
                );
                """;

            await using var connection =
                await _connectionFactory.CreateOpenConnectionAsync(
                    cancellationToken);

            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    sql,
                    new { Title = title, ExceptId = exceptId },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> UpdateAsync(
        Directorate directorate,
        CancellationToken cancellationToken = default)
        {
            const string sql = """
                UPDATE org.directorates
                SET directorate_title = @Title,
                    is_directorate_active = @IsActive
                WHERE directorate_id = @Id;
                """;

            await using var connection =
                await _connectionFactory.CreateOpenConnectionAsync(
                    cancellationToken);

            var affected = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        directorate.Id,
                        directorate.Title,
                        directorate.IsActive
                    },
                    cancellationToken: cancellationToken));

            return affected == 1;
        }

        private static Directorate ToDomain(DirectorateRow row) =>
        Directorate.Rehydrate(row.Id, row.Title, row.IsActive);

        private sealed class DirectorateRow
        {
            public int Id { get; init; }
            public string Title { get; init; } = "";
            public bool IsActive { get; init; }
        }
    }
}
