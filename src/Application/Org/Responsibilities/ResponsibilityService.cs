using Application.Abstractions;
using Domain.Org;
using static Domain.Org.Responsibility;

namespace Application.Org.Responsibilities;

public sealed class ResponsibilityService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "CEO",
        "DIRECTOR",
        "RISK_SPECIALIST"
    };

    private readonly IResponsibilityRepository _repository;

    public ResponsibilityService(IResponsibilityRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> CreateAsync(
        int personId,
        int? directedPersonId,
        string responsibilityType,
        CancellationToken cancellationToken = default)
    {
        Validate(personId, directedPersonId, responsibilityType);

        if (!await _repository.PersonExistsAsync(personId, cancellationToken))
            throw new ResponsibilityValidationException(
                $"Сотрудник с ID {personId} не найден или неактивен.");

        if (directedPersonId.HasValue
            && !await _repository.PersonExistsAsync(directedPersonId.Value, cancellationToken))
        {
            throw new ResponsibilityValidationException(
                $"Подчинённый сотрудник с ID {directedPersonId} не найден или неактивен.");
        }

        var responsibility = new Responsibility(
            personId,
            directedPersonId,
            responsibilityType.ToUpperInvariant());

        return await _repository.CreateAsync(responsibility, cancellationToken);
    }

    public Task<IReadOnlyList<Responsibility>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task<Responsibility> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ResponsibilityValidationException("Некорректный ID назначения.");

        return await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new ResponsibilityNotFoundException(id);
    }

    public async Task<int> UpdateAsync(
        int id,
        int? personId,
        int? directedPersonId,
        string? responsibilityType,
        bool? isResponsibilityActive,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ResponsibilityValidationException("Некорректный ID назначения.");

        if (personId is null
            && directedPersonId is null
            && responsibilityType is null
            && isResponsibilityActive is null)
        {
            throw new ResponsibilityValidationException("Не передано ни одного поля для обновления.");
        }

        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new ResponsibilityNotFoundException(id);

        // Валидация переданных полей
        if (personId.HasValue)
        {
            if (personId.Value <= 0)
                throw new ResponsibilityValidationException("personId должен быть положительным.");

            if (!await _repository.PersonExistsAsync(personId.Value, cancellationToken))
                throw new ResponsibilityValidationException(
                    $"Сотрудник с ID {personId} не найден или неактивен.");
        }

        if (directedPersonId.HasValue)
        {
            if (directedPersonId.Value <= 0)
                throw new ResponsibilityValidationException("directedId должен быть положительным.");

            if (!await _repository.PersonExistsAsync(directedPersonId.Value, cancellationToken))
                throw new ResponsibilityValidationException(
                    $"Подчинённый сотрудник с ID {directedPersonId} не найден или неактивен.");
        }

        if (responsibilityType is not null)
        {
            if (!AllowedTypes.Contains(responsibilityType.Trim()))
                throw new ResponsibilityValidationException(
                    "Недопустимый тип ответственности. Разрешены: CEO, DIRECTOR, RISK_SPECIALIST.");
        }

        var update = new ResponsibilityUpdate
        {
            Id = id,
            PersonId = personId,
            DirectedPersonId = directedPersonId,
            Type = responsibilityType?.Trim().ToUpperInvariant(),
            IsActive = isResponsibilityActive
        };

        var updated = await _repository.UpdateAsync(update, cancellationToken);
        if (!updated)
            throw new ResponsibilityNotFoundException(id);

        return id;
    }

    public async Task DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ResponsibilityValidationException("Некорректный ID назначения.");

        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new ResponsibilityNotFoundException(id);

        if (!await _repository.DeactivateAsync(id, cancellationToken))
            throw new ResponsibilityNotFoundException(id);
    }

    private static void Validate(
        int personId,
        int? directedPersonId,
        string responsibilityType)
    {
        if (personId <= 0)
            throw new ResponsibilityValidationException("personId должен быть положительным числом.");

        if (directedPersonId.HasValue && directedPersonId.Value <= 0)
            throw new ResponsibilityValidationException("directedId должен быть положительным числом.");

        if (string.IsNullOrWhiteSpace(responsibilityType))
            throw new ResponsibilityValidationException("Тип ответственности обязателен.");

        if (!AllowedTypes.Contains(responsibilityType.Trim()))
            throw new ResponsibilityValidationException(
                "Недопустимый тип ответственности. Разрешены: CEO, DIRECTOR, RISK_SPECIALIST.");

        // Для CEO directedId должен быть null
        if (string.Equals(responsibilityType.Trim(), "CEO", StringComparison.OrdinalIgnoreCase)
            && directedPersonId.HasValue)
        {
            throw new ResponsibilityValidationException(
                "Для типа CEO directedId должен быть пустым.");
        }
    }
}

public sealed class ResponsibilityValidationException(string message)
    : Exception(message);

public sealed class ResponsibilityNotFoundException(int id)
    : Exception($"Назначение с ID {id} не найдено.");

public sealed class ResponsibilityConflictException(string message)
    : Exception(message);