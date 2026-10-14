using Application.Abstractions;
using Domain.Org;

namespace Application.Org.Assets;

public sealed class AssetService
{
    private readonly IAssetRepository _repository;

    public AssetService(IAssetRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> CreateAsync(
        string assetTitle,
        string? assetInventoryNum,
        decimal assetHourCost,
        CancellationToken cancellationToken = default)
    {
        Validate(assetTitle, assetInventoryNum, assetHourCost);

        var asset = new Asset(assetTitle, assetInventoryNum, assetHourCost);

        return await _repository.CreateAsync(asset, cancellationToken);
    }

    public Task<IReadOnlyList<Asset>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task<Asset> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new AssetValidationException("Некорректный ID объекта.");

        return await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new AssetNotFoundException(id);
    }

    public async Task<int> UpdateAsync(
        int id,
        string? assetTitle,
        string? assetInventoryNum,
        decimal? assetHourCost,
        bool? isAssetActive,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new AssetValidationException("Некорректный ID объекта.");

        if (assetTitle is null
            && assetInventoryNum is null
            && assetHourCost is null
            && isAssetActive is null)
        {
            throw new AssetValidationException("Не передано ни одного поля для обновления.");
        }

        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new AssetNotFoundException(id);

        if (assetTitle is not null)
        {
            if (string.IsNullOrWhiteSpace(assetTitle))
                throw new AssetValidationException("Название объекта обязательно.");

            if (assetTitle.Trim().Length > 250)
                throw new AssetValidationException("Название объекта слишком длинное.");
        }

        if (assetInventoryNum is not null && assetInventoryNum.Trim().Length > 120)
            throw new AssetValidationException("Инвентарный номер слишком длинный.");

        if (assetHourCost.HasValue && assetHourCost.Value < 0)
            throw new AssetValidationException("Стоимость часа не может быть отрицательной.");

        // Если деактивируем — проверяем зависимости
        if (isAssetActive == false)
        {
            var dependencies = await _repository.GetDependenciesAsync(id, cancellationToken);
            if (dependencies.HasAny)
                throw new AssetHasDependenciesException(dependencies);
        }

        var update = new AssetUpdate
        {
            Id = id,
            Title = assetTitle?.Trim(),
            InventoryNumber = assetInventoryNum?.Trim(),
            HourCost = assetHourCost.HasValue
                ? decimal.Round(assetHourCost.Value, 2, MidpointRounding.AwayFromZero)
                : null,
            IsActive = isAssetActive
        };

        var updated = await _repository.UpdateAsync(update, cancellationToken);
        if (!updated)
            throw new AssetNotFoundException(id);

        return id;
    }

    public async Task DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new AssetValidationException("Некорректный ID объекта.");

        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new AssetNotFoundException(id);

        var dependencies = await _repository.GetDependenciesAsync(id, cancellationToken);
        if (dependencies.HasAny)
            throw new AssetHasDependenciesException(dependencies);

        if (!await _repository.DeactivateAsync(id, cancellationToken))
            throw new AssetNotFoundException(id);
    }

    private static void Validate(
        string assetTitle,
        string? assetInventoryNum,
        decimal assetHourCost)
    {
        if (string.IsNullOrWhiteSpace(assetTitle))
            throw new AssetValidationException("Название объекта обязательно.");

        if (assetTitle.Trim().Length > 250)
            throw new AssetValidationException("Название объекта не должно превышать 250 символов.");

        if (assetInventoryNum is not null && assetInventoryNum.Trim().Length > 120)
            throw new AssetValidationException("Инвентарный номер не должен превышать 120 символов.");

        if (assetHourCost < 0)
            throw new AssetValidationException("Стоимость часа не может быть отрицательной.");
    }
}

public sealed class AssetValidationException(string message)
    : Exception(message);

public sealed class AssetNotFoundException(int id)
    : Exception($"Объект с ID {id} не найден.");

public sealed class AssetConflictException(string message)
    : Exception(message);

public sealed class AssetHasDependenciesException(
    AssetDependencies dependencies)
    : Exception("Нельзя деактивировать объект: существуют блокирующие зависимости.")
{
    public AssetDependencies Dependencies { get; } = dependencies;
}