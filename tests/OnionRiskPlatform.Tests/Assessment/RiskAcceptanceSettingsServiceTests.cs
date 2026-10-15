using Application.Abstractions;
using Application.Assessment.AcceptanceSettings;
using Domain.Assessment;

namespace OnionRiskPlatform.Tests.Assessment;

public class RiskAcceptanceSettingsServiceTests
{
    [Fact]
    public async Task GetAsync_ValidMethodId_ReturnsSettings()
    {
        // Arrange
        var settings = RiskAcceptanceSettings.Rehydrate(1, 1, 5, 10, 20);
        var repo = new Mock<IRiskAcceptanceSettingsRepository>();
        repo.Setup(r => r.MethodExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.GetByMethodIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var service = new RiskAcceptanceSettingsService(repo.Object);

        // Act
        var result = await service.GetAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.OwnerMaxScore.Should().Be(5);
    }

    [Fact]
    public async Task GetAsync_MethodNotFound_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IRiskAcceptanceSettingsRepository>();
        repo.Setup(r => r.MethodExistsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new RiskAcceptanceSettingsService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AcceptanceSettingsValidationException>(() =>
            service.GetAsync(99));
    }

    [Fact]
    public async Task GetAsync_SettingsNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IRiskAcceptanceSettingsRepository>();
        repo.Setup(r => r.MethodExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.GetByMethodIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RiskAcceptanceSettings?)null);

        var service = new RiskAcceptanceSettingsService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AcceptanceSettingsNotFoundException>(() =>
            service.GetAsync(1));
    }

    [Fact]
    public async Task SaveAsync_ValidInput_ReturnsSettingId()
    {
        // Arrange
        var repo = new Mock<IRiskAcceptanceSettingsRepository>();
        repo.Setup(r => r.MethodExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.SaveAsync(It.IsAny<RiskAcceptanceSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var service = new RiskAcceptanceSettingsService(repo.Object);

        // Act
        var result = await service.SaveAsync(1, 5, 10, 20);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task SaveAsync_MethodNotFound_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IRiskAcceptanceSettingsRepository>();
        repo.Setup(r => r.MethodExistsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new RiskAcceptanceSettingsService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AcceptanceSettingsValidationException>(() =>
            service.SaveAsync(99, 5, 10, 20));
    }
}
