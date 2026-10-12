using Domain.Common;

namespace Domain.Org;

public sealed class Directorate : Entity
{
    public string Title { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private Directorate() { }

    public static Directorate Rehydrate(int id, string title, bool isActive)
    {
        var directorate = new Directorate(title);
        directorate.Id = id;
        directorate.SetActive(isActive);
        return directorate;
    }

    public Directorate(string title) => Rename(title);

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Название подразделения обязательно.");

        title = title.Trim();
        if (title.Length > 250)
            throw new DomainException("Название подразделения слишком длинное.");

        Title = title;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public sealed class Person : Entity
{
    public int? UserId { get; private set; }
    public int? DirectorateId { get; private set; }
    public string? Position { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Person() { }

    public Person(int? userId, int? directorateId, string? position)
    {
        if (userId is <= 0 || directorateId is <= 0)
            throw new DomainException("ID пользователя или подразделения некорректен.");

        UserId = userId;
        DirectorateId = directorateId;
        ChangePosition(position);
    }

    public void ChangePosition(string? position)
    {
        position = string.IsNullOrWhiteSpace(position) ? null : position.Trim();

        if (position?.Length > 250)
            throw new DomainException("Название должности слишком длинное.");

        Position = position;
    }

    public void ChangeDirectorate(int? directorateId)
    {
        if (directorateId is <= 0)
            throw new DomainException("ID подразделения должен быть положительным.");

        DirectorateId = directorateId;
    }

    public void LinkUser(int? userId)
    {
        if (userId is <= 0)
            throw new DomainException("ID пользователя должен быть положительным.");

        UserId = userId;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public sealed class BusinessProcess : Entity
{
    public int DirectorateId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int OwnerId { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    private BusinessProcess() { }

    public BusinessProcess(
        int directorateId,
        string title,
        int ownerId,
        string? description)
    {
        if (directorateId <= 0 || ownerId <= 0)
            throw new DomainException("Подразделение и владелец процесса обязательны.");

        DirectorateId = directorateId;
        OwnerId = ownerId;
        Rename(title);
        ChangeDescription(description);
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Название процесса обязательно.");

        title = title.Trim();
        if (title.Length > 250)
            throw new DomainException("Название процесса слишком длинное.");

        Title = title;
    }

    public void ChangeOwner(int ownerId)
    {
        if (ownerId <= 0)
            throw new DomainException("Владелец процесса указан некорректно.");

        OwnerId = ownerId;
    }

    public void ChangeDescription(string? description) =>
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public sealed class Asset : Entity
{
    public string Title { get; private set; } = string.Empty;
    public string? InventoryNumber { get; private set; }
    public decimal HourCost { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Asset() { }

    public Asset(string title, string? inventoryNumber, decimal hourCost)
    {
        Rename(title);
        ChangeInventoryNumber(inventoryNumber);
        ChangeHourCost(hourCost);
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Название актива обязательно.");

        title = title.Trim();
        if (title.Length > 250)
            throw new DomainException("Название актива слишком длинное.");

        Title = title;
    }

    public void ChangeInventoryNumber(string? number)
    {
        number = string.IsNullOrWhiteSpace(number) ? null : number.Trim();

        if (number?.Length > 120)
            throw new DomainException("Инвентарный номер слишком длинный.");

        InventoryNumber = number;
    }

    public void ChangeHourCost(decimal cost)
    {
        if (cost < 0)
            throw new DomainException("Стоимость часа не может быть отрицательной.");

        HourCost = decimal.Round(cost, 2, MidpointRounding.AwayFromZero);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public sealed class Responsibility : Entity
{
    public int PersonId { get; private set; }
    public int? DirectedPersonId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private Responsibility() { }

    public Responsibility(int personId, int? directedPersonId, string type)
    {
        if (personId <= 0 || directedPersonId is <= 0)
            throw new DomainException("ID сотрудника указан некорректно.");

        PersonId = personId;
        DirectedPersonId = directedPersonId;
        ChangeType(type);
    }

    public void ChangeType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new DomainException("Тип ответственности обязателен.");

        type = type.Trim();
        if (type.Length > 80)
            throw new DomainException("Тип ответственности слишком длинный.");

        Type = type;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}