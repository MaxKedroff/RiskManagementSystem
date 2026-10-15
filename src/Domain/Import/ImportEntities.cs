using Domain.Common;

namespace Domain.Import;

public enum ImportStatus
{
    Uploaded = 1,
    Processing = 2,
    Completed = 3,
    CompletedWithErrors = 4,
    Failed = 5
}

public sealed class FileUpload : Entity
{
    public string Title { get; private set; } = string.Empty;
    public ImportStatus Status { get; private set; } = ImportStatus.Uploaded;
    public int PersonId { get; private set; }
    public int TotalRows { get; private set; }
    public int ValidRows { get; private set; }
    public int InvalidRows { get; private set; }
    public string Report { get; private set; } = string.Empty;
    public string? Error { get; private set; }

    private FileUpload() { }

    public static FileUpload Rehydrate(
        int id, string title, ImportStatus status, int personId,
        int totalRows, int validRows, int invalidRows, string report, string? error)
    {
        var upload = new FileUpload(title, personId)
        {
            Id = id,
            Status = status,
            TotalRows = totalRows,
            ValidRows = validRows,
            InvalidRows = invalidRows,
            Report = report,
            Error = error
        };
        return upload;
    }

    public FileUpload(string title, int personId)
    {
        if (personId <= 0)
            throw new DomainException("Автор загрузки указан некорректно.");

        PersonId = personId;
        Rename(title);
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Название загрузки обязательно.");

        title = title.Trim();
        if (title.Length > 300)
            throw new DomainException("Название загрузки слишком длинное.");

        Title = title;
    }

    public void StartProcessing()
    {
        if (Status is not ImportStatus.Uploaded)
            throw new DomainException("Обрабатывать можно только новую загрузку.");

        Status = ImportStatus.Processing;
        Error = null;
    }

    public void Complete(int totalRows, int validRows, int invalidRows, string report)
    {
        EnsureProcessing();
        ValidateCounts(totalRows, validRows, invalidRows);

        TotalRows = totalRows;
        ValidRows = validRows;
        InvalidRows = invalidRows;
        Report = report ?? string.Empty;
        Error = null;
        Status = invalidRows == 0
            ? ImportStatus.Completed
            : ImportStatus.CompletedWithErrors;
    }

    public void Fail(string error)
    {
        EnsureProcessing();

        if (string.IsNullOrWhiteSpace(error))
            throw new DomainException("Для неудачного импорта необходимо указать ошибку.");

        Status = ImportStatus.Failed;
        Error = error.Trim();
    }

    private void EnsureProcessing()
    {
        if (Status != ImportStatus.Processing)
            throw new DomainException("Загрузка не находится в процессе обработки.");
    }

    private static void ValidateCounts(int total, int valid, int invalid)
    {
        if (total < 0 || valid < 0 || invalid < 0)
            throw new DomainException("Количество строк не может быть отрицательным.");

        if (valid + invalid != total)
            throw new DomainException("Сумма валидных и невалидных строк должна совпадать с общим количеством.");
    }
}