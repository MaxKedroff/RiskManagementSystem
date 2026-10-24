using Domain.Common;

namespace Domain.Assessment;

public sealed class AssessmentMethod : Entity
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private AssessmentMethod() { }

    public AssessmentMethod(string title, string? description = null)
    {
        Rename(title);
        ChangeDescription(description);
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Название метода оценки обязательно.");

        title = title.Trim();
        if (title.Length > 250)
            throw new DomainException("Название метода слишком длинное.");

        Title = title;
    }

    public void ChangeDescription(string? description) =>
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}

public sealed class LikelihoodLevel : Entity
{
    public int MethodId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Weight { get; private set; }

    private LikelihoodLevel() { }

    public LikelihoodLevel(int methodId, string name, int weight)
    {
        if (methodId <= 0 || weight < 0)
            throw new DomainException("Метод или вес вероятности указан некорректно.");

        MethodId = methodId;
        Rename(name);
        Weight = weight;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Название уровня вероятности обязательно.");

        name = name.Trim();
        if (name.Length > 200)
            throw new DomainException("Название уровня слишком длинное.");

        Name = name;
    }

    public void ChangeWeight(int weight)
    {
        if (weight < 0)
            throw new DomainException("Вес не может быть отрицательным.");

        Weight = weight;
    }
}

public sealed class ImpactLevel : Entity
{
    public int MethodId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Weight { get; private set; }
    public decimal Loss { get; private set; }
    public decimal MaximumLoss { get; private set; }

    private ImpactLevel() { }

    public static ImpactLevel Rehydrate(
        int id, int methodId, string name, int weight, decimal loss, decimal maximumLoss)
    {
        var level = new ImpactLevel(methodId, name, weight, loss, maximumLoss)
        {
            Id = id
        };
        return level;
    }

    public ImpactLevel(
        int methodId,
        string name,
        int weight,
        decimal loss,
        decimal maximumLoss)
    {
        if (methodId <= 0 || weight < 0)
            throw new DomainException("Метод или вес влияния указан некорректно.");

        MethodId = methodId;
        Rename(name);
        ChangeLossRange(loss, maximumLoss);
        Weight = weight;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Название уровня влияния обязательно.");

        name = name.Trim();
        if (name.Length > 200)
            throw new DomainException("Название уровня слишком длинное.");

        Name = name;
    }

    public void ChangeWeight(int weight)
    {
        if (weight < 0)
            throw new DomainException("Вес не может быть отрицательным.");

        Weight = weight;
    }

    public void ChangeLossRange(decimal loss, decimal maximumLoss)
    {
        if (loss < 0 || maximumLoss < loss)
            throw new DomainException("Диапазон финансовых потерь некорректен.");

        Loss = decimal.Round(loss, 2, MidpointRounding.AwayFromZero);
        MaximumLoss = decimal.Round(maximumLoss, 2, MidpointRounding.AwayFromZero);
    }
}

public sealed class RiskAssessment : Entity
{
    public int RiskId { get; private set; }
    public int MethodId { get; private set; }
    public int LikelihoodLevelId { get; private set; }
    public int ImpactLevelId { get; private set; }

    public decimal LossAmount { get; private set; }
    public decimal? DowntimeWeeks { get; private set; }
    public decimal? DowntimeDays { get; private set; }
    public decimal? DowntimeHours { get; private set; }
    public decimal? TotalDowntimeHours { get; private set; }
    public decimal? HourlyDowntimeCost { get; private set; }

    public int RiskScore { get; private set; }
    public string? ResponseMode { get; private set; }
    public bool IsActive { get; private set; }
    public int CreatedById { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private RiskAssessment() { }

    public RiskAssessment(
        int riskId,
        int methodId,
        int likelihoodLevelId,
        int impactLevelId,
        decimal lossAmount,
        int riskScore,
        int createdById,
        decimal? downtimeWeeks = null,
        decimal? downtimeDays = null,
        decimal? downtimeHours = null,
        decimal? totalDowntimeHours = null,
        decimal? hourlyDowntimeCost = null,
        string? responseMode = null)
    {
        if (riskId <= 0 || methodId <= 0 ||
            likelihoodLevelId <= 0 || impactLevelId <= 0 || createdById <= 0)
            throw new DomainException("Не заполнены обязательные ссылки оценки.");

        if (lossAmount < 0 || riskScore < 0)
            throw new DomainException("Потери и балл риска не могут быть отрицательными.");

        ValidateNonNegative(downtimeWeeks, nameof(downtimeWeeks));
        ValidateNonNegative(downtimeDays, nameof(downtimeDays));
        ValidateNonNegative(downtimeHours, nameof(downtimeHours));
        ValidateNonNegative(totalDowntimeHours, nameof(totalDowntimeHours));
        ValidateNonNegative(hourlyDowntimeCost, nameof(hourlyDowntimeCost));

        RiskId = riskId;
        MethodId = methodId;
        LikelihoodLevelId = likelihoodLevelId;
        ImpactLevelId = impactLevelId;
        LossAmount = RoundMoney(lossAmount);
        RiskScore = riskScore;
        CreatedById = createdById;
        DowntimeWeeks = downtimeWeeks;
        DowntimeDays = downtimeDays;
        DowntimeHours = downtimeHours;
        TotalDowntimeHours = totalDowntimeHours;
        HourlyDowntimeCost = hourlyDowntimeCost is null
            ? null
            : RoundMoney(hourlyDowntimeCost.Value);
        ResponseMode = string.IsNullOrWhiteSpace(responseMode) ? null : responseMode.Trim();
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate() => IsActive = false;

    public void SetResponseMode(string mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            throw new DomainException("Режим ответа обязателен.");

        ResponseMode = mode.Trim();
    }

    private static decimal RoundMoney(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private static void ValidateNonNegative(decimal? value, string name)
    {
        if (value < 0)
            throw new DomainException($"{name} не может быть отрицательным.");
    }
}

public sealed class RiskAcceptanceSettings : Entity
{
    public int MethodId { get; private set; }
    public int OwnerMaxScore { get; private set; }
    public int DirectorMaxScore { get; private set; }
    public int CeoMaxScore { get; private set; }

    private RiskAcceptanceSettings() { }

    public static RiskAcceptanceSettings Rehydrate(
        int id, int methodId, int owner, int director, int ceo)
    {
        var settings = new RiskAcceptanceSettings(methodId, owner, director, ceo)
        {
            Id = id
        };
        return settings;
    }

    public RiskAcceptanceSettings(
        int methodId,
        int ownerMaxScore,
        int directorMaxScore,
        int ceoMaxScore)
    {
        if (methodId <= 0)
            throw new DomainException("Метод оценки обязателен.");

        MethodId = methodId;
        ChangeThresholds(ownerMaxScore, directorMaxScore, ceoMaxScore);
    }

    public void ChangeThresholds(int owner, int director, int ceo)
    {
        if (owner < 0 || director < owner || ceo < director)
            throw new DomainException(
                "Пороги должны быть неотрицательными и идти по возрастанию.");

        OwnerMaxScore = owner;
        DirectorMaxScore = director;
        CeoMaxScore = ceo;
    }
}