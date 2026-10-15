using Application.Abstractions;
using Domain.Risk;

namespace Application.Risk.Categories;

public sealed class RiskCategoryService
{
    private readonly IRiskCategoryRepository _repository;

    public RiskCategoryService(IRiskCategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> CreateAsync(
        string categoryName,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = ValidateName(categoryName);

        if (await _repository.NameExistsAsync(
            normalizedName, cancellationToken: cancellationToken))
        {
            throw new RiskCategoryConflictException(
                "Категория с таким названием уже существует.");
        }

        var category = new RiskCategory(normalizedName);

        return await _repository.CreateAsync(category, cancellationToken);
    }

    public Task<IReadOnlyList<RiskCategory>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task<RiskCategory> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new RiskCategoryValidationException("Некорректный ID категории.");

        return await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new RiskCategoryNotFoundException(id);
    }

    public async Task<int> UpdateAsync(
        int id,
        string? categoryName,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new RiskCategoryValidationException("Некорректный ID категории.");

        if (categoryName is null && isActive is null)
        {
            throw new RiskCategoryValidationException("Не передано ни одного поля для обновления.");
        }

        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new RiskCategoryNotFoundException(id);

        if (categoryName is not null)
        {
            var normalizedName = ValidateName(categoryName);

            if (await _repository.NameExistsAsync(normalizedName, id, cancellationToken))
            {
                throw new RiskCategoryConflictException(
                    "Категория с таким названием уже существует.");
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
            throw new RiskCategoryValidationException("Некорректный ID категории.");

        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new RiskCategoryNotFoundException(id);

        var dependencies = await _repository.GetDependenciesAsync(id, cancellationToken);
        if (dependencies.HasAny)
            throw new RiskCategoryHasDependenciesException(dependencies);

        existing.Deactivate();

        await _repository.DeactivateAsync(id, cancellationToken);
    }

    private static string ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new RiskCategoryValidationException("Название категории обязательно.");

        var normalized = name.Trim();

        if (normalized.Length > 250)
            throw new RiskCategoryValidationException(
                "Название категории не должно превышать 250 символов.");

        return normalized;
    }
}

public sealed class RiskCategoryValidationException(string message) : Exception(message);

public sealed class RiskCategoryNotFoundException(int id)
    : Exception($"Категория с ID {id} не найдена.");

public sealed class RiskCategoryConflictException(string message) : Exception(message);

public sealed class RiskCategoryHasDependenciesException(
    RiskCategoryDependencies dependencies)
    : Exception("Нельзя деактивировать категорию: существуют связанные риски.")
{
    public RiskCategoryDependencies Dependencies { get; } = dependencies;
}
