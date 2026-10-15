using Domain.Import;

namespace Application.Abstractions;

public interface IFileUploadRepository
{
    Task<int> CreateAsync(
        FileUpload upload,
        CancellationToken cancellationToken = default);

    Task<FileUpload?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateStatusAsync(
        int id,
        ImportStatus status,
        string? error = null,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteAsync(
        int id,
        int totalRows,
        int validRows,
        int invalidRows,
        string report,
        CancellationToken cancellationToken = default);
}
