using Domain.Org;

namespace Application.Abstractions
{
    public interface IAssetRepository
    {
        Task<int> CreateAsync(Asset asset, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<Asset?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(AssetUpdate update, CancellationToken cancellationToken = default);

        Task<bool> DeactivateAsync(int assetId, CancellationToken cancellationToken = default);

        Task<AssetDependencies> GetDependenciesAsync(int assetId, CancellationToken cancellationToken = default);
    }
}
