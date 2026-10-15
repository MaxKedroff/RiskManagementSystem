using Application.Abstractions;
using Application.Risk.HazardGroups;
using Domain.Risk;

namespace OnionRiskPlatform.Tests.Risk;

public class HazardGroupServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var repo = new Mock<IHazardGroupRepository>();
        repo.Setup(r => r.NameExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.CreateAsync(It.IsAny<HazardGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var service = new HazardGroupService(repo.Object);

        // Act
        var result = await service.CreateAsync("Physical");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IHazardGroupRepository>();
        var service = new HazardGroupService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardGroupValidationException>(() =>
            service.CreateAsync(""));
    }

    [Fact]
    public async Task CreateAsync_TooLongName_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IHazardGroupRepository>();
        var service = new HazardGroupService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardGroupValidationException>(() =>
            service.CreateAsync(new string('a', 251)));
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        var repo = new Mock<IHazardGroupRepository>();
        repo.Setup(r => r.NameExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new HazardGroupService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardGroupConflictException>(() =>
            service.CreateAsync("Existing"));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllHazardGroups()
    {
        // Arrange
        var groups = new List<HazardGroup>
        {
            HazardGroup.Rehydrate(1, "Physical", true),
            HazardGroup.Rehydrate(2, "Financial", true)
        };
        var repo = new Mock<IHazardGroupRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HazardGroup>)groups);

        var service = new HazardGroupService(repo.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsHazardGroup()
    {
        // Arrange
        var group = HazardGroup.Rehydrate(1, "Physical", true);
        var repo = new Mock<IHazardGroupRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var service = new HazardGroupService(repo.Object);

        // Act
        var result = await service.GetByIdAsync(1);

        // Assert
        result.Should().Be(group);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IHazardGroupRepository>();
        var service = new HazardGroupService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardGroupValidationException>(() =>
            service.GetByIdAsync(0));
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IHazardGroupRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HazardGroup?)null);

        var service = new HazardGroupService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardGroupNotFoundException>(() =>
            service.GetByIdAsync(99));
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var group = HazardGroup.Rehydrate(1, "Old", true);
        var repo = new Mock<IHazardGroupRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        repo.Setup(r => r.NameExistsAsync(It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.UpdateAsync(It.IsAny<HazardGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new HazardGroupService(repo.Object);

        // Act
        var result = await service.UpdateAsync(1, "New Name", true);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_NoFields_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IHazardGroupRepository>();
        var service = new HazardGroupService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardGroupValidationException>(() =>
            service.UpdateAsync(1, null, null));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IHazardGroupRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HazardGroup?)null);

        var service = new HazardGroupService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardGroupNotFoundException>(() =>
            service.UpdateAsync(99, "New", true));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        var group = HazardGroup.Rehydrate(1, "Old", true);
        var repo = new Mock<IHazardGroupRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        repo.Setup(r => r.NameExistsAsync(It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new HazardGroupService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardGroupConflictException>(() =>
            service.UpdateAsync(1, "Existing", true));
    }

    [Fact]
    public async Task DeactivateAsync_ValidInput_Deactivates()
    {
        // Arrange
        var group = HazardGroup.Rehydrate(1, "Test", true);
        var repo = new Mock<IHazardGroupRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        repo.Setup(r => r.GetDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HazardGroupDependencies { Hazards = 0 });
        repo.Setup(r => r.DeactivateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new HazardGroupService(repo.Object);

        // Act
        await service.DeactivateAsync(1);

        // Assert
        group.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IHazardGroupRepository>();
        var service = new HazardGroupService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardGroupValidationException>(() =>
            service.DeactivateAsync(0));
    }

    [Fact]
    public async Task DeactivateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IHazardGroupRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HazardGroup?)null);

        var service = new HazardGroupService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardGroupNotFoundException>(() =>
            service.DeactivateAsync(99));
    }

    [Fact]
    public async Task DeactivateAsync_HasDependencies_ThrowsHasDependenciesException()
    {
        // Arrange
        var group = HazardGroup.Rehydrate(1, "Test", true);
        var repo = new Mock<IHazardGroupRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        repo.Setup(r => r.GetDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HazardGroupDependencies { Hazards = 5 });

        var service = new HazardGroupService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HazardGroupHasDependenciesException>(() =>
            service.DeactivateAsync(1));
    }
}
