using Application.Abstractions;
using Application.Risk.Hazards;
using Domain.Risk;

namespace OnionRiskPlatform.Tests.Risk;

public class HazardServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var repo = new Mock<IHazardRepository>();
        repo.Setup(r => r.HazardGroupIsActiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.NameExistsAsync(It.IsAny<string>(), 1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.CreateAsync(It.IsAny<Hazard>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var service = new HazardService(repo.Object);

        // Act
        var result = await service.CreateAsync(1, "Physical");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IHazardRepository>();
        var service = new HazardService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardValidationException>(() =>
            service.CreateAsync(1, ""));
    }

    [Fact]
    public async Task CreateAsync_TooLongName_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IHazardRepository>();
        var service = new HazardService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardValidationException>(() =>
            service.CreateAsync(1, new string('a', 251)));
    }

    [Fact]
    public async Task CreateAsync_InactiveGroup_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IHazardRepository>();
        repo.Setup(r => r.HazardGroupIsActiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new HazardService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardValidationException>(() =>
            service.CreateAsync(1, "Physical"));
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        var repo = new Mock<IHazardRepository>();
        repo.Setup(r => r.HazardGroupIsActiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.NameExistsAsync(It.IsAny<string>(), 1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new HazardService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardConflictException>(() =>
            service.CreateAsync(1, "Existing"));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllHazards()
    {
        // Arrange
        var hazards = new List<Hazard>
        {
            Hazard.Rehydrate(1, 1, "Physical1", true),
            Hazard.Rehydrate(2, 1, "Physical2", true)
        };
        var repo = new Mock<IHazardRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Hazard>)hazards);

        var service = new HazardService(repo.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsHazard()
    {
        // Arrange
        var hazard = Hazard.Rehydrate(1, 1, "Physical", true);
        var repo = new Mock<IHazardRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hazard);

        var service = new HazardService(repo.Object);

        // Act
        var result = await service.GetByIdAsync(1);

        // Assert
        result.Should().Be(hazard);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IHazardRepository>();
        var service = new HazardService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardValidationException>(() =>
            service.GetByIdAsync(0));
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IHazardRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hazard?)null);

        var service = new HazardService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardNotFoundException>(() =>
            service.GetByIdAsync(99));
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var hazard = Hazard.Rehydrate(1, 1, "Old", true);
        var repo = new Mock<IHazardRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hazard);
        repo.Setup(r => r.HazardGroupIsActiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.NameExistsAsync(It.IsAny<string>(), 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Hazard>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new HazardService(repo.Object);

        // Act
        var result = await service.UpdateAsync(1, 1, "New Name", true);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_NoFields_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IHazardRepository>();
        var service = new HazardService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardValidationException>(() =>
            service.UpdateAsync(1, null, null, null));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IHazardRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hazard?)null);

        var service = new HazardService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardNotFoundException>(() =>
            service.UpdateAsync(99, 1, "New", true));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        var hazard = Hazard.Rehydrate(1, 1, "Old", true);
        var repo = new Mock<IHazardRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hazard);
        repo.Setup(r => r.HazardGroupIsActiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.NameExistsAsync(It.IsAny<string>(), 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new HazardService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardConflictException>(() =>
            service.UpdateAsync(1, 1, "Existing", true));
    }

    [Fact]
    public async Task UpdateAsync_InactiveGroup_ThrowsValidationException()
    {
        // Arrange
        var hazard = Hazard.Rehydrate(1, 1, "Old", true);
        var repo = new Mock<IHazardRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hazard);
        repo.Setup(r => r.HazardGroupIsActiveAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new HazardService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardValidationException>(() =>
            service.UpdateAsync(1, 2, "Name", true));
    }

    [Fact]
    public async Task DeactivateAsync_ValidInput_Deactivates()
    {
        // Arrange
        var hazard = Hazard.Rehydrate(1, 1, "Test", true);
        var repo = new Mock<IHazardRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hazard);
        repo.Setup(r => r.GetDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HazardDependencies { Risks = 0 });
        repo.Setup(r => r.DeactivateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new HazardService(repo.Object);

        // Act
        await service.DeactivateAsync(1);

        // Assert - DeactivateAsync was called on the repo
        repo.Verify(r => r.DeactivateAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IHazardRepository>();
        var service = new HazardService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardValidationException>(() =>
            service.DeactivateAsync(0));
    }

    [Fact]
    public async Task DeactivateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IHazardRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hazard?)null);

        var service = new HazardService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardNotFoundException>(() =>
            service.DeactivateAsync(99));
    }

    [Fact]
    public async Task DeactivateAsync_HasDependencies_ThrowsHasDependenciesException()
    {
        // Arrange
        var hazard = Hazard.Rehydrate(1, 1, "Test", true);
        var repo = new Mock<IHazardRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hazard);
        repo.Setup(r => r.GetDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HazardDependencies { Risks = 5 });

        var service = new HazardService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardHasDependenciesException>(() =>
            service.DeactivateAsync(1));
    }
}
