using Application.Abstractions;
using Domain.Risk;

namespace Application.Risk.Hazards;

public sealed class HazardService
{
    private readonly IHazardRepository _repository;

    public HazardService(IHazardRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> CreateAsync(
        int hazardGroupId,
        string hazardName,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = ValidateName(hazardName);

        if (!await _repository.HazardGroupIsActiveAsync(
            hazardGroupId, cancellationToken))
        {
            throw new HazardValidationException(
                $"Активная группа опасностей с ID {hazardGroupId} не найдена.");
        }

        if (await _repository.NameExistsAsync(
            normalizedName, hazardGroupId, cancellationToken: cancellationToken))
        {
            throw new HazardConflictException(
                "Опасность с таким названием уже существует в данной группе.");
        }

        var hazard = new Hazard(hazardGroupId, normalizedName);

        return await _repository.CreateAsync(hazard, cancellationToken);
    }

    public Task<IReadOnlyList<Hazard>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task<Hazard> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new HazardValidationException("Некорректный ID опасности.");

        return await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new HazardNotFoundException(id);
    }

    public async Task<int> UpdateAsync(
        int id,
        int? hazardGroupId,
        string? hazardName,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new HazardValidationException("Некорректный ID опасности.");

        if (hazardGroupId is null && hazardName is null && isActive is null)
        {
            throw new HazardValidationException("Не передано ни одного поля для обновления.");
        }

        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new HazardNotFoundException(id);

        if (hazardGroupId.HasValue)
        {
            if (hazardGroupId.Value <= 0)
                throw new HazardValidationException("hazardGroupId должен быть положительным.");

            if (!await _repository.HazardGroupIsActiveAsync(
                hazardGroupId.Value, cancellationToken))
            {
                throw new HazardValidationException(
                    $"Активная группа опасностей с ID {hazardGroupId.Value} не найдена.");
            }

            existing.MoveToGroup(hazardGroupId.Value);
        }

        if (hazardName is not null)
        {
            var normalizedName = ValidateName(hazardName);

            if (await _repository.NameExistsAsync(
                normalizedName, existing.HazardGroupId, id, cancellationToken))
            {
                throw new HazardConflictException(
                    "Опасность с таким названием уже существует в данной группе.");
            }

            existing.Rename(normalizedName);
        }

        if (isActive.HasValue)
        {
            existing.SetActive(isActive.Value);
        }

        await _repository.UpdateAsync(existing, cancellationToken);

        return id;
    }

    public async Task DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new HazardValidationException("Некорректный ID опасности.");

        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new HazardNotFoundException(id);

        var dependencies = await _repository.GetDependenciesAsync(id, cancellationToken);
        if (dependencies.HasAny)
            throw new HazardHasDependenciesException(dependencies);

        existing.Deactivate();

        await _repository.DeactivateAsync(id, cancellationToken);
    }

    private static string ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new HazardValidationException("Название опасности обязательно.");

        var normalized = name.Trim();

        if (normalized.Length > 250)
            throw new HazardValidationException(
                "Название опасности не должно превышать 250 символов.");

        return normalized;
    }
}

public sealed class HazardValidationException(string message) : Exception(message);

public sealed class HazardNotFoundException(int id)
    : Exception($"Опасность с ID {id} не найдена.");

public sealed class HazardConflictException(string message) : Exception(message);

public sealed class HazardHasDependenciesException(
    HazardDependencies dependencies)
    : Exception("Нельзя деактивировать опасность: существуют связанные риски.")
{
    public HazardDependencies Dependencies { get; } = dependencies;
}
