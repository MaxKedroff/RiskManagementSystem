using Application.Abstractions;
using Domain.Import;

namespace Application.Import.FileUploads;

public sealed class FileUploadService
{
    private readonly IFileUploadRepository _repository;

    public FileUploadService(IFileUploadRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> UploadAsync(
        string title,
        int personId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new FileUploadValidationException("Название загрузки обязательно.");

        if (personId <= 0)
            throw new FileUploadValidationException("personId должен быть положительным.");

        var upload = new FileUpload(title, personId);

        return await _repository.CreateAsync(upload, cancellationToken);
    }

    public async Task<FileUploadResponse> ConfirmAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var upload = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new FileUploadNotFoundException(id);

        upload.StartProcessing();

        // Simulate parsing: count all rows as valid for now
        var totalRows = 0;
        var validRows = 0;
        var invalidRows = 0;
        var report = string.Empty;

        await _repository.CompleteAsync(
            id, totalRows, validRows, invalidRows, report, cancellationToken);

        return new FileUploadResponse(
            upload.Id,
            upload.Title,
            (int)upload.Status,
            upload.TotalRows,
            upload.ValidRows,
            upload.InvalidRows,
            upload.Report,
            upload.Error);
    }
}

public sealed record FileUploadResponse(
    int FileUploadId,
    string FileUploadTitle,
    int FileUploadStatus,
    int FileUploadRows,
    int FileUploadRowsValid,
    int FileUploadRowsInvalid,
    string FileUploadReport,
    string? FileUploadError);

public sealed record UploadFileRequest(
    string Title,
    int PersonId);

public sealed class FileUploadValidationException(string message) : Exception(message);

public sealed class FileUploadNotFoundException(int id)
    : Exception($"Загрузка с ID {id} не найдена.");
