using Application.Abstractions;
using Domain.Treatment;
using Domain.Common;

namespace Application.Treatment.ApprovalSettings;

public sealed class TreatmentApprovalSettingsService
{
    private readonly ITreatmentApprovalSettingsRepository _repository;

    public TreatmentApprovalSettingsService(ITreatmentApprovalSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<TreatmentApprovalSettingsResponse> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetAsync(cancellationToken);

        if (settings is null)
            throw new ApprovalSettingsNotFoundException();

        return new TreatmentApprovalSettingsResponse(
            settings.Id,
            settings.OwnerThreshold,
            settings.DirectorThreshold,
            settings.CeoThreshold);
    }

    public async Task<int> SaveAsync(
        int ownerThreshold,
        int directorThreshold,
        int ceoThreshold,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = new TreatmentApprovalSettings(
                ownerThreshold, directorThreshold, ceoThreshold);

            return await _repository.SaveAsync(settings, cancellationToken);
        }
        catch (DomainException ex)
        {
            throw new ApprovalSettingsValidationException(ex.Message);
        }
    }
}

public sealed record TreatmentApprovalSettingsResponse(
    int TreatmentApprovalSetId,
    int OwnerThreshold,
    int DirectorThreshold,
    int CeoThreshold);

public sealed record SaveApprovalSettingsRequest(
    int OwnerThreshold,
    int DirectorThreshold,
    int CeoThreshold);

public sealed class ApprovalSettingsValidationException(string message) : Exception(message);

public sealed class ApprovalSettingsNotFoundException : Exception
{
    public ApprovalSettingsNotFoundException()
        : base("Настройки согласования не найдены.")
    { }
}
