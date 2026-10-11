using Dapper;
using Npgsql;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Database
{
    public sealed class DatabaseInitializer
    {
        private const string LockName = "risk-platform:migrations";

        private readonly NpgsqlDataSource _dataSource;

        public DatabaseInitializer(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task InitializeAsync(
            CancellationToken cancellationToken = default)
        {
            await using var connection =
                await _dataSource.OpenConnectionAsync(cancellationToken);

            await connection.ExecuteAsync(new CommandDefinition(
                """
            CREATE SCHEMA IF NOT EXISTS infrastructure;

            CREATE TABLE IF NOT EXISTS infrastructure.schema_migrations
            (
                version      varchar(200) PRIMARY KEY,
                sha256       varchar(64) NOT NULL,
                applied_at   timestamptz NOT NULL DEFAULT now()
            );
            """,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                "SELECT pg_advisory_lock(hashtext(@LockName));",
                new { LockName },
                cancellationToken: cancellationToken));

            try
            {
                await ApplyMigrationsAsync(connection, cancellationToken);
            }
            finally
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    "SELECT pg_advisory_unlock(hashtext(@LockName));",
                    new { LockName },
                    cancellationToken: CancellationToken.None));
            }
        }

        private static async Task ApplyMigrationsAsync(
            NpgsqlConnection connection,
            CancellationToken cancellationToken)
        {
            var assembly = typeof(DatabaseInitializer).Assembly;

            var migrations = assembly
                .GetManifestResourceNames()
                .Where(name => name.Contains(
                    ".Database.Migrations.",
                    StringComparison.Ordinal))
                .Where(name => name.EndsWith(
                    ".sql",
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            foreach (var resourceName in migrations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var version = Path.GetFileNameWithoutExtension(
                    resourceName.Split('.').TakeLast(2).First());

                // В именах embedded resources точки каталогов и файла
                // могут совпадать. Версия извлекается из имени миграции.
                version = ExtractMigrationVersion(resourceName);

                string sql;
                await using (var stream =
                    assembly.GetManifestResourceStream(resourceName)
                    ?? throw new InvalidOperationException(
                        $"Не найдена встроенная миграция {resourceName}."))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    sql = await reader.ReadToEndAsync(cancellationToken);
                }

                var hash = Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(sql)));

                var existingHash = await connection.QuerySingleOrDefaultAsync<string>(
                    new CommandDefinition(
                        """
                    SELECT sha256
                    FROM infrastructure.schema_migrations
                    WHERE version = @Version;
                    """,
                        new { Version = version },
                        cancellationToken: cancellationToken));

                if (existingHash is not null)
                {
                    if (!string.Equals(
                        existingHash,
                        hash,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Миграция {version} уже применена, но её содержимое изменилось. " +
                            "Создайте новую миграцию.");
                    }

                    continue;
                }

                await using var transaction =
                    await connection.BeginTransactionAsync(cancellationToken);

                try
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        sql,
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                    await connection.ExecuteAsync(new CommandDefinition(
                        """
                    INSERT INTO infrastructure.schema_migrations(version, sha256)
                    VALUES (@Version, @Hash);
                    """,
                        new { Version = version, Hash = hash },
                        transaction,
                        cancellationToken: cancellationToken));

                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    throw;
                }
            }
        }

        private static string ExtractMigrationVersion(string resourceName)
        {
            const string marker = ".Database.Migrations.";

            var markerIndex = resourceName.IndexOf(
                marker,
                StringComparison.Ordinal);

            if (markerIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Неожиданное имя ресурса миграции: {resourceName}");
            }

            var fileName = resourceName[(markerIndex + marker.Length)..];

            const string extension = ".sql";

            if (!fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Неизвестное расширение миграции: {resourceName}");
            }

            return fileName[..^extension.Length];
        }
    }
}
