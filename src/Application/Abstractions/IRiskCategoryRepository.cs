using Domain.Risk;

namespace Application.Abstractions;

public interface IRiskCategoryRepository
{
    Task<int> CreateAsync(
        RiskCategory category,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RiskCategory>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<RiskCategory?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string name,
        int? exceptId = null,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        RiskCategory category,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<RiskCategoryDependencies> GetDependenciesAsync(
        int categoryId,
        CancellationToken cancellationToken = default);
}
