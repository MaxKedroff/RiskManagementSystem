using Domain.Common;

namespace Domain.Treatment;

public enum TreatmentStatus
{
    Planned = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum ApprovalStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum ApprovalLevel
{
    Owner = 1,
    Director = 2,
    Ceo = 3
}

public sealed class Treatment : Entity
{
    public int RiskProcessId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TreatmentStatus Status { get; private set; } = TreatmentStatus.Planned;
    public DateTimeOffset DueAt { get; private set; }
    public decimal PlannedCost { get; private set; }
    public decimal ActualCost { get; private set; }
    public int CreatedById { get; private set; }
    public int? AssignedToId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Treatment() { }

    public Treatment(
        int riskProcessId,
        string title,
        string? description,
        DateTimeOffset dueAt,
        decimal plannedCost,
        int createdById,
        int? assignedToId = null)
    {
        if (riskProcessId <= 0 || createdById <= 0 || assignedToId is <= 0)
            throw new DomainException("Ссылка на риск или пользователя некорректна.");

        if (plannedCost < 0)
            throw new DomainException("Плановая стоимость не может быть отрицательной.");

        RiskProcessId = riskProcessId;
        CreatedById = createdById;
        AssignedToId = assignedToId;
        DueAt = dueAt;
        PlannedCost = Money(plannedCost);
        ActualCost = 0;
        CreatedAt = DateTimeOffset.UtcNow;

        Rename(title);
        ChangeDescription(description);
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Название мероприятия обязательно.");

        title = title.Trim();
        if (title.Length > 300)
            throw new DomainException("Название мероприятия слишком длинное.");

        Title = title;
        Touch();
    }

    public void ChangeDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Touch();
    }

    public void Reschedule(DateTimeOffset dueAt)
    {
        EnsureNotFinal();
        DueAt = dueAt;
        Touch();
    }

    public void AssignTo(int? userId)
    {
        EnsureNotFinal();

        if (userId is <= 0)
            throw new DomainException("Исполнитель указан некорректно.");

        AssignedToId = userId;
        Touch();
    }

    public void SetActualCost(decimal actualCost)
    {
        if (actualCost < 0)
            throw new DomainException("Фактическая стоимость не может быть отрицательной.");

        ActualCost = Money(actualCost);
        Touch();
    }

    public void Start()
    {
        if (Status != TreatmentStatus.Planned)
            throw new DomainException("Начать можно только запланированное мероприятие.");

        Status = TreatmentStatus.InProgress;
        Touch();
    }

    public void Complete()
    {
        if (Status != TreatmentStatus.InProgress)
            throw new DomainException("Завершить можно только выполняемое мероприятие.");

        Status = TreatmentStatus.Completed;
        Touch();
    }

    public void Cancel()
    {
        EnsureNotFinal();
        Status = TreatmentStatus.Cancelled;
        Touch();
    }

    private void EnsureNotFinal()
    {
        if (Status is TreatmentStatus.Completed or TreatmentStatus.Cancelled)
            throw new DomainException("Завершённое или отменённое мероприятие нельзя изменить.");
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private static decimal Money(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed class ApprovalRequest : Entity
{
    public int TreatmentId { get; private set; }
    public ApprovalLevel RequiredLevel { get; private set; }
    public ApprovalStatus Status { get; private set; } = ApprovalStatus.Pending;
    public int CreatedById { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset? DecisionAt { get; private set; }
    public string? Reason { get; private set; }
    public int? DecidedById { get; private set; }

    private ApprovalRequest() { }

    public ApprovalRequest(int treatmentId, ApprovalLevel requiredLevel, int createdById)
    {
        if (treatmentId <= 0 || createdById <= 0)
            throw new DomainException("Мероприятие и автор согласования обязательны.");

        if (!Enum.IsDefined(requiredLevel))
            throw new DomainException("Неизвестный уровень согласования.");

        TreatmentId = treatmentId;
        RequiredLevel = requiredLevel;
        CreatedById = createdById;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void AddComment(string? comment)
    {
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }

    public void Approve(int approverId, string? reason = null)
    {
        Decide(approverId, ApprovalStatus.Approved, reason);
    }

    public void Reject(int approverId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("При отклонении необходимо указать причину.");

        Decide(approverId, ApprovalStatus.Rejected, reason);
    }

    public void Cancel()
    {
        EnsurePending();
        Status = ApprovalStatus.Cancelled;
        DecisionAt = DateTimeOffset.UtcNow;
    }

    private void Decide(int approverId, ApprovalStatus status, string? reason)
    {
        EnsurePending();

        if (approverId <= 0)
            throw new DomainException("Согласующий указан некорректно.");

        Status = status;
        DecidedById = approverId;
        DecisionAt = DateTimeOffset.UtcNow;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    private void EnsurePending()
    {
        if (Status != ApprovalStatus.Pending)
            throw new DomainException("Запрос согласования уже обработан.");
    }
}

public sealed class TreatmentApprovalSettings : Entity
{
    public int OwnerThreshold { get; private set; }
    public int DirectorThreshold { get; private set; }
    public int CeoThreshold { get; private set; }

    private TreatmentApprovalSettings() { }

    public static TreatmentApprovalSettings Rehydrate(
        int id, int owner, int director, int ceo)
    {
        var settings = new TreatmentApprovalSettings(owner, director, ceo)
        {
            Id = id
        };
        return settings;
    }

    public TreatmentApprovalSettings(int owner, int director, int ceo) =>
        ChangeThresholds(owner, director, ceo);

    public void ChangeThresholds(int owner, int director, int ceo)
    {
        if (owner < 0 || director < owner || ceo < director)
            throw new DomainException(
                "Пороги согласования должны быть неотрицательными и идти по возрастанию.");

        OwnerThreshold = owner;
        DirectorThreshold = director;
        CeoThreshold = ceo;
    }
}