using Application.Abstractions;
using Domain.Assessment;

namespace Application.Assessment.AcceptanceSettings;

public sealed class RiskAcceptanceSettingsService
{
    private readonly IRiskAcceptanceSettingsRepository _repository;

    public RiskAcceptanceSettingsService(IRiskAcceptanceSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<RiskAcceptanceSettingsResponse> GetAsync(
        int methodId,
        CancellationToken cancellationToken = default)
    {
        if (!await _repository.MethodExistsAsync(methodId, cancellationToken))
            throw new AcceptanceSettingsValidationException(
                $"Метод оценки с ID {methodId} не найден.");

        var settings = await _repository.GetByMethodIdAsync(methodId, cancellationToken);

        if (settings is null)
            throw new AcceptanceSettingsNotFoundException(methodId);

        return new RiskAcceptanceSettingsResponse(
            settings.Id,
            settings.OwnerMaxScore,
            settings.DirectorMaxScore,
            settings.CeoMaxScore);
    }

    public async Task<int> SaveAsync(
        int methodId,
        int ownerMaxScore,
        int directorMaxScore,
        int ceoMaxScore,
        CancellationToken cancellationToken = default)
    {
        if (!await _repository.MethodExistsAsync(methodId, cancellationToken))
            throw new AcceptanceSettingsValidationException(
                $"Метод оценки с ID {methodId} не найден.");

        var settings = new RiskAcceptanceSettings(
            methodId, ownerMaxScore, directorMaxScore, ceoMaxScore);

        return await _repository.SaveAsync(settings, cancellationToken);
    }
}

public sealed record RiskAcceptanceSettingsResponse(
    int SettingId,
    int OwnerMaxScore,
    int DirectorMaxScore,
    int CeoMaxScore);

public sealed record SaveAcceptanceSettingsRequest(
    int MethodId,
    int OwnerMaxScore,
    int DirectorMaxScore,
    int CeoMaxScore);

public sealed class AcceptanceSettingsValidationException(string message) : Exception(message);

public sealed class AcceptanceSettingsNotFoundException(int methodId)
    : Exception($"Настройки принятия для метода с ID {methodId} не найдены.");
