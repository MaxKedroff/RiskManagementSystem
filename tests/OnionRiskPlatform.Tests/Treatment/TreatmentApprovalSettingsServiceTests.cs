using Application.Abstractions;
using Application.Treatment.ApprovalSettings;
using Domain.Treatment;

namespace OnionRiskPlatform.Tests.Treatment;

public class TreatmentApprovalSettingsServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsSettings()
    {
        // Arrange
        var settings = TreatmentApprovalSettings.Rehydrate(1, 5, 10, 20);
        var repo = new Mock<ITreatmentApprovalSettingsRepository>();
        repo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var service = new TreatmentApprovalSettingsService(repo.Object);

        // Act
        var result = await service.GetAsync();

        // Assert
        result.Should().NotBeNull();
        result.OwnerThreshold.Should().Be(5);
    }

    [Fact]
    public async Task GetAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<ITreatmentApprovalSettingsRepository>();
        repo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((TreatmentApprovalSettings?)null);

        var service = new TreatmentApprovalSettingsService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ApprovalSettingsNotFoundException>(() =>
            service.GetAsync());
    }

    [Fact]
    public async Task SaveAsync_ValidInput_ReturnsSettingId()
    {
        // Arrange
        var repo = new Mock<ITreatmentApprovalSettingsRepository>();
        repo.Setup(r => r.SaveAsync(It.IsAny<TreatmentApprovalSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var service = new TreatmentApprovalSettingsService(repo.Object);

        // Act
        var result = await service.SaveAsync(5, 10, 20);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task SaveAsync_NegativeOwner_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<ITreatmentApprovalSettingsRepository>();
        var service = new TreatmentApprovalSettingsService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ApprovalSettingsValidationException>(() =>
            service.SaveAsync(-1, 10, 20));
    }

    [Fact]
    public async Task SaveAsync_DirectorLessThanOwner_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<ITreatmentApprovalSettingsRepository>();
        var service = new TreatmentApprovalSettingsService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ApprovalSettingsValidationException>(() =>
            service.SaveAsync(10, 5, 20));
    }

    [Fact]
    public async Task SaveAsync_CeoLessThanDirector_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<ITreatmentApprovalSettingsRepository>();
        var service = new TreatmentApprovalSettingsService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ApprovalSettingsValidationException>(() =>
            service.SaveAsync(5, 10, 3));
    }
}
