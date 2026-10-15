using Application.Abstractions;
using Application.Org.Assets;
using Domain.Org;

namespace OnionRiskPlatform.Tests.Org;

public class AssetServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var repo = new Mock<IAssetRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<Asset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var service = new AssetService(repo.Object);

        // Act
        var result = await service.CreateAsync("Test Asset", "INV-001", 100.50m);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IAssetRepository>();
        var service = new AssetService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AssetValidationException>(() =>
            service.CreateAsync("", "INV-001", 100.50m));
    }

    [Fact]
    public async Task CreateAsync_NegativeHourCost_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IAssetRepository>();
        var service = new AssetService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AssetValidationException>(() =>
            service.CreateAsync("Asset", "INV-001", -10));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllAssets()
    {
        // Arrange
        var assets = new List<Asset>
        {
            Asset.Rehydrate(1, "Asset1", "INV-1", 100, true),
            Asset.Rehydrate(2, "Asset2", "INV-2", 200, true)
        };
        var repo = new Mock<IAssetRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Asset>)assets);

        var service = new AssetService(repo.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsAsset()
    {
        // Arrange
        var asset = Asset.Rehydrate(1, "Test", "INV-1", 100, true);
        var repo = new Mock<IAssetRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        var service = new AssetService(repo.Object);

        // Act
        var result = await service.GetByIdAsync(1);

        // Assert
        result.Should().Be(asset);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IAssetRepository>();
        var service = new AssetService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AssetValidationException>(() =>
            service.GetByIdAsync(0));
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IAssetRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Asset?)null);

        var service = new AssetService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AssetNotFoundException>(() =>
            service.GetByIdAsync(99));
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var asset = Asset.Rehydrate(1, "Old", "INV-1", 100, true);
        var repo = new Mock<IAssetRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);
        repo.Setup(r => r.UpdateAsync(It.IsAny<AssetUpdate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new AssetService(repo.Object);

        // Act
        var result = await service.UpdateAsync(1, "New", "INV-2", 200, true);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_NoFields_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IAssetRepository>();
        var service = new AssetService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AssetValidationException>(() =>
            service.UpdateAsync(1, null, null, null, null));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IAssetRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Asset?)null);

        var service = new AssetService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AssetNotFoundException>(() =>
            service.UpdateAsync(99, "New", "INV", 100, true));
    }

    [Fact]
    public async Task UpdateAsync_NegativeHourCost_ThrowsValidationException()
    {
        // Arrange
        var asset = Asset.Rehydrate(1, "Test", "INV-1", 100, true);
        var repo = new Mock<IAssetRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        var service = new AssetService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AssetValidationException>(() =>
            service.UpdateAsync(1, "Test", "INV", -10, true));
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingWithDependencies_ThrowsHasDependenciesException()
    {
        // Arrange
        var asset = Asset.Rehydrate(1, "Test", "INV-1", 100, true);
        var repo = new Mock<IAssetRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);
        repo.Setup(r => r.GetDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDependencies { Risks = 1 });

        var service = new AssetService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AssetHasDependenciesException>(() =>
            service.UpdateAsync(1, "Test", "INV", 100, false));
    }

    [Fact]
    public async Task DeactivateAsync_ValidInput_Deactivates()
    {
        // Arrange
        var asset = Asset.Rehydrate(1, "Test", "INV-1", 100, true);
        var repo = new Mock<IAssetRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);
        repo.Setup(r => r.GetDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDependencies { Risks = 0 });
        repo.Setup(r => r.DeactivateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new AssetService(repo.Object);

        // Act
        await service.DeactivateAsync(1);

        // Assert - DeactivateAsync was called on the repo
        repo.Verify(r => r.DeactivateAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IAssetRepository>();
        var service = new AssetService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AssetValidationException>(() =>
            service.DeactivateAsync(0));
    }

    [Fact]
    public async Task DeactivateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IAssetRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Asset?)null);

        var service = new AssetService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AssetNotFoundException>(() =>
            service.DeactivateAsync(99));
    }

    [Fact]
    public async Task DeactivateAsync_HasDependencies_ThrowsHasDependenciesException()
    {
        // Arrange
        var asset = Asset.Rehydrate(1, "Test", "INV-1", 100, true);
        var repo = new Mock<IAssetRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);
        repo.Setup(r => r.GetDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDependencies { Risks = 1 });

        var service = new AssetService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AssetHasDependenciesException>(() =>
            service.DeactivateAsync(1));
    }
}
