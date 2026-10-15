using Application.Abstractions;
using Application.Assessment.ImpactLevels;
using Domain.Assessment;

namespace OnionRiskPlatform.Tests.Assessment;

public class ImpactLevelServiceTests
{
    [Fact]
    public async Task GetAllAsync_ValidMethodId_ReturnsLevels()
    {
        // Arrange
        var levels = new List<ImpactLevel>
        {
            ImpactLevel.Rehydrate(1, 1, "Low", 1, 0, 10000),
            ImpactLevel.Rehydrate(2, 1, "High", 2, 10000, 1000000)
        };
        var repo = new Mock<IImpactLevelRepository>();
        repo.Setup(r => r.MethodExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.GetAllAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ImpactLevel>)levels);

        var service = new ImpactLevelService(repo.Object);

        // Act
        var result = await service.GetAllAsync(1);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_InvalidMethodId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IImpactLevelRepository>();
        repo.Setup(r => r.MethodExistsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new ImpactLevelService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ImpactLevelValidationException>(() =>
            service.GetAllAsync(99));
    }

    [Fact]
    public async Task SaveAsync_ValidInput_ReturnsMethodId()
    {
        // Arrange
        var repo = new Mock<IImpactLevelRepository>();
        repo.Setup(r => r.SaveAsync(It.IsAny<IReadOnlyList<ImpactLevel>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ImpactLevelService(repo.Object);
        var levels = new[]
        {
            new ImpactLevelUpdateRequest(null, 0, 10000),
            new ImpactLevelUpdateRequest(null, 10000, 1000000)
        };

        // Act
        var result = await service.SaveAsync(1, levels);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task SaveAsync_EmptyLevels_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IImpactLevelRepository>();
        var service = new ImpactLevelService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ImpactLevelValidationException>(() =>
            service.SaveAsync(1, Array.Empty<ImpactLevelUpdateRequest>()));
    }

    [Fact]
    public async Task SaveAsync_NegativeLoss_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IImpactLevelRepository>();
        var service = new ImpactLevelService(repo.Object);
        var levels = new[] { new ImpactLevelUpdateRequest(null, -100, 10000) };

        // Act & Assert
        await Assert.ThrowsAsync<ImpactLevelValidationException>(() =>
            service.SaveAsync(1, levels));
    }

    [Fact]
    public async Task SaveAsync_MaxLessThanLoss_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IImpactLevelRepository>();
        var service = new ImpactLevelService(repo.Object);
        var levels = new[] { new ImpactLevelUpdateRequest(null, 10000, 5000) };

        // Act & Assert
        await Assert.ThrowsAsync<ImpactLevelValidationException>(() =>
            service.SaveAsync(1, levels));
    }

    [Fact]
    public async Task SaveAsync_OutOfOrderLoss_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IImpactLevelRepository>();
        var service = new ImpactLevelService(repo.Object);
        var levels = new[]
        {
            new ImpactLevelUpdateRequest(null, 0, 10000),
            new ImpactLevelUpdateRequest(null, 5000, 20000) // 5000 < 10000 previous max
        };

        // Act & Assert
        await Assert.ThrowsAsync<ImpactLevelValidationException>(() =>
            service.SaveAsync(1, levels));
    }

    [Fact]
    public async Task SaveAsync_WithExistingIds_UpdatesCorrectly()
    {
        // Arrange
        var repo = new Mock<IImpactLevelRepository>();
        repo.Setup(r => r.SaveAsync(It.IsAny<IReadOnlyList<ImpactLevel>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ImpactLevelService(repo.Object);
        var levels = new[]
        {
            new ImpactLevelUpdateRequest(1, 0, 10000),
            new ImpactLevelUpdateRequest(2, 10000, 1000000)
        };

        // Act
        var result = await service.SaveAsync(1, levels);

        // Assert
        result.Should().Be(1);
    }
}
