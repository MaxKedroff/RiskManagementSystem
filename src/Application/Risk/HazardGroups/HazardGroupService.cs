using Application.Abstractions;
using Domain.Risk;

namespace Application.Risk.HazardGroups;

public sealed class HazardGroupService
{
    private readonly IHazardGroupRepository _repository;

    public HazardGroupService(IHazardGroupRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> CreateAsync(
        string hazardGroupName,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = ValidateName(hazardGroupName);

        if (await _repository.NameExistsAsync(
            normalizedName,
            cancellationToken: cancellationToken))
        {
            throw new HazardGroupConflictException(
                "Группа опасностей с таким названием уже существует.");
        }

        var hazardGroup = new HazardGroup(normalizedName);

        return await _repository.CreateAsync(hazardGroup, cancellationToken);
    }

    public Task<IReadOnlyList<HazardGroup>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task<HazardGroup> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new HazardGroupValidationException("Некорректный ID группы опасностей.");

        return await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new HazardGroupNotFoundException(id);
    }

    public async Task<int> UpdateAsync(
        int id,
        string? hazardGroupName,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new HazardGroupValidationException("Некорректный ID группы опасностей.");

        if (hazardGroupName is null && isActive is null)
        {
            throw new HazardGroupValidationException("Не передано ни одного поля для обновления.");
        }

        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new HazardGroupNotFoundException(id);

        if (hazardGroupName is not null)
        {
            var normalizedName = ValidateName(hazardGroupName);

            if (await _repository.NameExistsAsync(normalizedName, id, cancellationToken))
            {
                throw new HazardGroupConflictException(
                    "Группа опасностей с таким названием уже существует.");
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
            throw new HazardGroupValidationException("Некорректный ID группы опасностей.");

        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new HazardGroupNotFoundException(id);

        var dependencies = await _repository.GetDependenciesAsync(id, cancellationToken);
        if (dependencies.HasAny)
            throw new HazardGroupHasDependenciesException(dependencies);

        existing.Deactivate();

        await _repository.DeactivateAsync(id, cancellationToken);
    }

    private static string ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new HazardGroupValidationException("Название группы опасностей обязательно.");

        var normalized = name.Trim();

        if (normalized.Length > 250)
            throw new HazardGroupValidationException(
                "Название группы опасностей не должно превышать 250 символов.");

        return normalized;
    }
}

public sealed class HazardGroupValidationException(string message) : Exception(message);

public sealed class HazardGroupNotFoundException(int id)
    : Exception($"Группа опасностей с ID {id} не найдена.");

public sealed class HazardGroupConflictException(string message) : Exception(message);

public sealed class HazardGroupHasDependenciesException(
    HazardGroupDependencies dependencies)
    : Exception("Нельзя деактивировать группу опасностей: существуют связанные опасности.")
{
    public HazardGroupDependencies Dependencies { get; } = dependencies;
}
