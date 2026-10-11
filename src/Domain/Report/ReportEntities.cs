using Domain.Common;

namespace Domain.Report;

public enum ReportStatus
{
    Pending = 1,
    Generating = 2,
    Completed = 3,
    Failed = 4
}

public enum ReportFormat
{
    Pdf = 1,
    Xlsx = 2,
    Csv = 3
}

public sealed class ReportTemplate : Entity
{
    public string Title { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private ReportTemplate() { }

    public ReportTemplate(string title) => Rename(title);

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Название шаблона обязательно.");

        title = title.Trim();
        if (title.Length > 250)
            throw new DomainException("Название шаблона слишком длинное.");

        Title = title;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public sealed class GeneratedReport : Entity
{
    public int TemplateId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public ReportStatus Status { get; private set; } = ReportStatus.Pending;
    public ReportFormat Format { get; private set; }
    public string Path { get; private set; } = string.Empty;
    public int GeneratedById { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }

    private GeneratedReport() { }

    public GeneratedReport(
        int templateId,
        string title,
        ReportFormat format,
        int generatedById)
    {
        if (templateId <= 0 || generatedById <= 0)
            throw new DomainException("Шаблон и автор отчёта обязательны.");

        if (!Enum.IsDefined(format))
            throw new DomainException("Неподдерживаемый формат отчёта.");

        TemplateId = templateId;
        GeneratedById = generatedById;
        Format = format;
        Rename(title);
    }

    public void StartGeneration()
    {
        if (Status != ReportStatus.Pending)
            throw new DomainException("Запустить можно только ожидающий отчёт.");

        Status = ReportStatus.Generating;
    }

    public void Complete(string path)
    {
        if (Status != ReportStatus.Generating)
            throw new DomainException("Отчёт не находится в процессе генерации.");

        if (string.IsNullOrWhiteSpace(path))
            throw new DomainException("Не указан путь к сформированному отчёту.");

        Path = path.Trim();
        GeneratedAt = DateTimeOffset.UtcNow;
        Status = ReportStatus.Completed;
    }

    public void Fail()
    {
        if (Status != ReportStatus.Generating)
            throw new DomainException("Отчёт не находится в процессе генерации.");

        Status = ReportStatus.Failed;
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Название отчёта обязательно.");

        title = title.Trim();
        if (title.Length > 300)
            throw new DomainException("Название отчёта слишком длинное.");

        Title = title;
    }
}