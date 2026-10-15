using Application.Abstractions;
using Dapper;
using Domain.Import;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public sealed class FileUploadRepository : IFileUploadRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FileUploadRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(
        FileUpload upload,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO import_data.file_upload
                (file_upload_title, file_upload_status, file_upload_person_id,
                 file_upload_rows, file_upload_rows_valid, file_upload_rows_invalid,
                 file_upload_report, file_upload_error)
            VALUES
                (@Title, 1, @PersonId, 0, 0, 0, '', NULL)
            RETURNING file_upload_id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new { Title = upload.Title, PersonId = upload.PersonId },
                cancellationToken: cancellationToken));
    }

    public async Task<FileUpload?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                file_upload_id AS Id,
                file_upload_title AS Title,
                file_upload_status AS Status,
                file_upload_person_id AS PersonId,
                file_upload_rows AS TotalRows,
                file_upload_rows_valid AS ValidRows,
                file_upload_rows_invalid AS InvalidRows,
                file_upload_report AS Report,
                file_upload_error AS Error
            FROM import_data.file_upload
            WHERE file_upload_id = @Id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<Row>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<bool> UpdateStatusAsync(
        int id,
        ImportStatus status,
        string? error = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE import_data.file_upload
            SET file_upload_status = @Status,
                file_upload_error = @Error
            WHERE file_upload_id = @Id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { Id = id, Status = (int)status, Error = (object?)error ?? DBNull.Value },
                cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<bool> CompleteAsync(
        int id,
        int totalRows,
        int validRows,
        int invalidRows,
        string report,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE import_data.file_upload
            SET file_upload_status = 3,
                file_upload_rows = @TotalRows,
                file_upload_rows_valid = @ValidRows,
                file_upload_rows_invalid = @InvalidRows,
                file_upload_report = @Report,
                file_upload_error = NULL
            WHERE file_upload_id = @Id;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Id = id,
                    TotalRows = totalRows,
                    ValidRows = validRows,
                    InvalidRows = invalidRows,
                    Report = report
                },
                cancellationToken: cancellationToken));

        return affected > 0;
    }

    private static FileUpload Map(Row row) =>
        FileUpload.Rehydrate(
            row.Id, row.Title, (ImportStatus)row.Status, row.PersonId,
            row.TotalRows, row.ValidRows, row.InvalidRows, row.Report, row.Error);

    private sealed class Row
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public int Status { get; init; }
        public int PersonId { get; init; }
        public int TotalRows { get; init; }
        public int ValidRows { get; init; }
        public int InvalidRows { get; init; }
        public string Report { get; init; } = string.Empty;
        public string? Error { get; init; }
    }
}
