using Application.Abstractions;
using Application.Org.Responsibilities;
using Domain.Org;

namespace OnionRiskPlatform.Tests.Org;

public class ResponsibilityServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var repo = new Mock<IResponsibilityRepository>();
        repo.Setup(r => r.PersonExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.PersonExistsAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.CreateAsync(It.IsAny<Responsibility>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var service = new ResponsibilityService(repo.Object);

        // Act
        var result = await service.CreateAsync(1, 2, "DIRECTOR");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task CreateAsync_ValidCEOInput_ReturnsId()
    {
        // Arrange
        var repo = new Mock<IResponsibilityRepository>();
        repo.Setup(r => r.PersonExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.CreateAsync(It.IsAny<Responsibility>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var service = new ResponsibilityService(repo.Object);

        // Act
        var result = await service.CreateAsync(1, null, "CEO");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task CreateAsync_InvalidPersonId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IResponsibilityRepository>();
        var service = new ResponsibilityService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ResponsibilityValidationException>(() =>
            service.CreateAsync(0, 1, "DIRECTOR"));
    }

    [Fact]
    public async Task CreateAsync_EmptyType_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IResponsibilityRepository>();
        var service = new ResponsibilityService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ResponsibilityValidationException>(() =>
            service.CreateAsync(1, 1, ""));
    }

    [Fact]
    public async Task CreateAsync_InvalidType_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IResponsibilityRepository>();
        var service = new ResponsibilityService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ResponsibilityValidationException>(() =>
            service.CreateAsync(1, 1, "INVALID"));
    }

    [Fact]
    public async Task CreateAsync_CEOWithDirectedId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IResponsibilityRepository>();
        var service = new ResponsibilityService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ResponsibilityValidationException>(() =>
            service.CreateAsync(1, 2, "CEO"));
    }

    [Fact]
    public async Task CreateAsync_PersonNotFound_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IResponsibilityRepository>();
        repo.Setup(r => r.PersonExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new ResponsibilityService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ResponsibilityValidationException>(() =>
            service.CreateAsync(1, 2, "DIRECTOR"));
    }

    [Fact]
    public async Task CreateAsync_DirectedPersonNotFound_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IResponsibilityRepository>();
        repo.Setup(r => r.PersonExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.PersonExistsAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new ResponsibilityService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ResponsibilityValidationException>(() =>
            service.CreateAsync(1, 2, "DIRECTOR"));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllResponsibilities()
    {
        // Arrange
        var responsibilities = new List<Responsibility>
        {
            Responsibility.Rehydrate(1, 1, 2, "DIRECTOR", true),
            Responsibility.Rehydrate(2, 3, null, "CEO", true)
        };
        var repo = new Mock<IResponsibilityRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Responsibility>)responsibilities);

        var service = new ResponsibilityService(repo.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsResponsibility()
    {
        // Arrange
        var resp = Responsibility.Rehydrate(1, 1, 2, "DIRECTOR", true);
        var repo = new Mock<IResponsibilityRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resp);

        var service = new ResponsibilityService(repo.Object);

        // Act
        var result = await service.GetByIdAsync(1);

        // Assert
        result.Should().Be(resp);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IResponsibilityRepository>();
        var service = new ResponsibilityService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ResponsibilityValidationException>(() =>
            service.GetByIdAsync(0));
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IResponsibilityRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Responsibility?)null);

        var service = new ResponsibilityService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ResponsibilityNotFoundException>(() =>
            service.GetByIdAsync(99));
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var resp = Responsibility.Rehydrate(1, 1, 2, "DIRECTOR", true);
        var repo = new Mock<IResponsibilityRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resp);
        repo.Setup(r => r.PersonExistsAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Responsibility.ResponsibilityUpdate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new ResponsibilityService(repo.Object);

        // Act
        var result = await service.UpdateAsync(1, 3, null, "CEO", true);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_NoFields_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IResponsibilityRepository>();
        var service = new ResponsibilityService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ResponsibilityValidationException>(() =>
            service.UpdateAsync(1, null, null, null, null));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IResponsibilityRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Responsibility?)null);

        var service = new ResponsibilityService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ResponsibilityNotFoundException>(() =>
            service.UpdateAsync(99, 1, 2, "DIRECTOR", true));
    }

    [Fact]
    public async Task UpdateAsync_InvalidType_ThrowsValidationException()
    {
        // Arrange
        var resp = Responsibility.Rehydrate(1, 1, 2, "DIRECTOR", true);
        var repo = new Mock<IResponsibilityRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resp);

        var service = new ResponsibilityService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ResponsibilityValidationException>(() =>
            service.UpdateAsync(1, 1, 2, "INVALID", true));
    }

    [Fact]
    public async Task DeactivateAsync_ValidInput_Deactivates()
    {
        // Arrange
        var resp = Responsibility.Rehydrate(1, 1, 2, "DIRECTOR", true);
        var repo = new Mock<IResponsibilityRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resp);
        repo.Setup(r => r.DeactivateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new ResponsibilityService(repo.Object);

        // Act
        await service.DeactivateAsync(1);

        // Assert - DeactivateAsync was called on the repo
        repo.Verify(r => r.DeactivateAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IResponsibilityRepository>();
        var service = new ResponsibilityService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ResponsibilityValidationException>(() =>
            service.DeactivateAsync(0));
    }

    [Fact]
    public async Task DeactivateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IResponsibilityRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Responsibility?)null);
        repo.Setup(r => r.DeactivateAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new ResponsibilityService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ResponsibilityNotFoundException>(() =>
            service.DeactivateAsync(99));
    }
}
