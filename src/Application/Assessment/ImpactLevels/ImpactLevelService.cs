using Application.Abstractions;
using Domain.Assessment;

namespace Application.Assessment.ImpactLevels;

public sealed class ImpactLevelService
{
    private readonly IImpactLevelRepository _repository;

    public ImpactLevelService(IImpactLevelRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ImpactLevelResponse>> GetAllAsync(
        int methodId,
        CancellationToken cancellationToken = default)
    {
        if (!await _repository.MethodExistsAsync(methodId, cancellationToken))
            throw new ImpactLevelValidationException(
                $"Метод оценки с ID {methodId} не найден.");

        var levels = await _repository.GetAllAsync(methodId, cancellationToken);

        return levels.Select(l => new ImpactLevelResponse(
            l.Id,
            l.Name,
            l.Weight,
            l.Loss,
            l.MaximumLoss)).ToArray();
    }

    public async Task<int> SaveAsync(
        int methodId,
        IReadOnlyList<ImpactLevelUpdateRequest> levels,
        CancellationToken cancellationToken = default)
    {
        if (levels is null || levels.Count == 0)
            throw new ImpactLevelValidationException("Необходимо указать уровни тяжести.");

        // Validate each level
        var previousMax = (decimal?)null;
        var mapped = new List<ImpactLevel>();

        for (var i = 0; i < levels.Count; i++)
        {
            var level = levels[i];

            if (level.ImpactLevelLoss < 0)
                throw new ImpactLevelValidationException(
                    $"Уровень {i + 1}: impactLevelLoss не может быть отрицательным.");

            if (level.ImpactLevelMax < level.ImpactLevelLoss)
                throw new ImpactLevelValidationException(
                    $"Уровень {i + 1}: impactLevelMax должен быть >= impactLevelLoss.");

            // Strict order: each level's loss must be > previous level's max
            if (previousMax.HasValue && level.ImpactLevelLoss < previousMax.Value)
                throw new ImpactLevelValidationException(
                    $"Уровень {i + 1}: impactLevelLoss должен быть > предыдущего impactLevelMax.");

            previousMax = level.ImpactLevelMax;

            if (level.ImpactLevelId.HasValue)
            {
                // Update existing - use Rehydrate pattern
                var existing = ImpactLevel.Rehydrate(
                    level.ImpactLevelId.Value,
                    methodId,
                    $"ImpactLevel{level.ImpactLevelId.Value}", 0,
                    level.ImpactLevelLoss, level.ImpactLevelMax);
                mapped.Add(existing);
            }
            else
            {
                // Create new
                mapped.Add(new ImpactLevel(
                    methodId,
                    $"Level{i + 1}", 0,
                    level.ImpactLevelLoss,
                    level.ImpactLevelMax));
            }
        }

        await _repository.SaveAsync(mapped, cancellationToken);

        return methodId;
    }
}

public sealed record ImpactLevelResponse(
    int ImpactLevelId,
    string ImpactLevelName,
    int ImpactLevelWeight,
    decimal ImpactLevelLoss,
    decimal ImpactLevelMax);

public sealed record ImpactLevelUpdateRequest(
    int? ImpactLevelId,
    decimal ImpactLevelLoss,
    decimal ImpactLevelMax);

public sealed class ImpactLevelValidationException(string message) : Exception(message);
