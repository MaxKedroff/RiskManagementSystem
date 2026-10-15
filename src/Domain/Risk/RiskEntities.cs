using Domain.Common;

namespace Domain.Risk;

public enum RiskStatus
{
    Draft = 1,
    Open = 2,
    InTreatment = 3,
    Accepted = 4,
    Closed = 5
}

public enum RiskAcceptanceLevel
{
    Owner = 1,
    Director = 2,
    Ceo = 3
}

public sealed class HazardGroup : Entity
{
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private HazardGroup() { }

    public static HazardGroup Rehydrate(int id, string name, bool isActive)
    {
        var hazardGroup = new HazardGroup(name) { Id = id };
        if (!isActive) hazardGroup.Deactivate();
        return hazardGroup;
    }

    public HazardGroup(string name) => Rename(name);

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Название группы опасностей обязательно.");

        name = name.Trim();
        if (name.Length > 250)
            throw new DomainException("Название группы слишком длинное.");

        Name = name;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void SetActive(bool isActive) => IsActive = isActive;
}

public sealed class Hazard : Entity
{
    public int HazardGroupId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private Hazard() { }

    public Hazard(int hazardGroupId, string name)
    {
        if (hazardGroupId <= 0)
            throw new DomainException("Группа опасности обязательна.");

        HazardGroupId = hazardGroupId;
        Rename(name);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Название опасности обязательно.");

        name = name.Trim();
        if (name.Length > 250)
            throw new DomainException("Название опасности слишком длинное.");

        Name = name;
    }

    public void MoveToGroup(int groupId)
    {
        if (groupId <= 0)
            throw new DomainException("Группа опасности указана некорректно.");

        HazardGroupId = groupId;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public sealed class RiskCategory : Entity
{
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private RiskCategory() { }

    public RiskCategory(string name) => Rename(name);

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Название категории обязательно.");

        name = name.Trim();
        if (name.Length > 250)
            throw new DomainException("Название категории слишком длинное.");

        Name = name;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public sealed class Risk : Entity
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int HazardId { get; private set; }
    public int CategoryId { get; private set; }
    public int AuthorId { get; private set; }
    public int? AssetId { get; private set; }
    public RiskStatus Status { get; private set; } = RiskStatus.Draft;
    public RiskAcceptanceLevel? AcceptanceLevel { get; private set; }
    public string? CloseReason { get; private set; }

    private Risk() { }

    public Risk(
        string title,
        string description,
        int hazardId,
        int categoryId,
        int authorId,
        int? assetId = null)
    {
        Rename(title);
        ChangeDescription(description);

        if (hazardId <= 0 || categoryId <= 0 || authorId <= 0 || assetId is <= 0)
            throw new DomainException("Одна из ссылок риска указана некорректно.");

        HazardId = hazardId;
        CategoryId = categoryId;
        AuthorId = authorId;
        AssetId = assetId;
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Название риска обязательно.");

        title = title.Trim();
        if (title.Length > 300)
            throw new DomainException("Название риска слишком длинное.");

        Title = title;
    }

    public void ChangeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Описание риска обязательно.");

        Description = description.Trim();
    }

    public void ChangeClassification(int hazardId, int categoryId, int? assetId)
    {
        if (hazardId <= 0 || categoryId <= 0 || assetId is <= 0)
            throw new DomainException("Классификация риска указана некорректно.");

        HazardId = hazardId;
        CategoryId = categoryId;
        AssetId = assetId;
    }

    public void StartTreatment()
    {
        if (Status is not (RiskStatus.Open or RiskStatus.Draft))
            throw new DomainException("Нельзя начать обработку риска в текущем статусе.");

        Status = RiskStatus.InTreatment;
    }

    public void Accept(RiskAcceptanceLevel level)
    {
        if (Status == RiskStatus.Closed)
            throw new DomainException("Закрытый риск нельзя принять.");

        if (!Enum.IsDefined(level))
            throw new DomainException("Неизвестный уровень принятия риска.");

        AcceptanceLevel = level;
        Status = RiskStatus.Accepted;
    }

    public void Reopen()
    {
        if (Status == RiskStatus.Closed)
        {
            Status = RiskStatus.Open;
            CloseReason = null;
            AcceptanceLevel = null;
        }
    }

    public void Close(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Для закрытия риска укажите причину.");

        if (Status == RiskStatus.Closed)
            throw new DomainException("Риск уже закрыт.");

        CloseReason = reason.Trim();
        Status = RiskStatus.Closed;
    }
}

public sealed class RiskProcess : Entity
{
    public int RiskId { get; private set; }
    public int ProcessedOwnerUserId { get; private set; }
    public int PersonId { get; private set; }
    public bool IsPrimary { get; private set; }

    private RiskProcess() { }

    public RiskProcess(
        int riskId,
        int processedOwnerUserId,
        int personId,
        bool isPrimary = false)
    {
        if (riskId <= 0 || processedOwnerUserId <= 0 || personId <= 0)
            throw new DomainException("Связь риска с ответственным указана некорректно.");

        RiskId = riskId;
        ProcessedOwnerUserId = processedOwnerUserId;
        PersonId = personId;
        IsPrimary = isPrimary;
    }

    public void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
    public void ChangePerson(int personId)
    {
        if (personId <= 0)
            throw new DomainException("Сотрудник указан некорректно.");

        PersonId = personId;
    }
}

public sealed class RiskComment : Entity
{
    public int AuthorId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public int EntityId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private RiskComment() { }

    public RiskComment(int authorId, string text, string entityType, int entityId)
    {
        if (authorId <= 0 || entityId <= 0)
            throw new DomainException("Автор и объект комментария обязательны.");

        AuthorId = authorId;
        EntityId = entityId;
        EntityType = Required(entityType, 50, nameof(entityType));
        Text = Required(text, int.MaxValue, nameof(text));
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Edit(string text)
    {
        Text = Required(text, int.MaxValue, nameof(text));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string Required(string value, int max, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{name} не может быть пустым.");

        value = value.Trim();
        if (value.Length > max)
            throw new DomainException($"{name} слишком длинный.");

        return value;
    }
}

public sealed class HazardGroupDependencies
{
    public long Hazards { get; init; }

    public bool HasAny => Hazards > 0;
}