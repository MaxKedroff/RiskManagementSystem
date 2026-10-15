using Application.Abstractions;
using Application.Org.Directorates;
using Domain.Org;
using Moq;

namespace OnionRiskPlatform.Tests.Org;

public class DirectorateServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var repo = new Mock<IDirectorateRepository>();
        repo.Setup(r => r.TitleExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.CreateAsync(It.IsAny<Directorate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var service = new DirectorateService(repo.Object);

        // Act
        var result = await service.CreateAsync("Test Directorate");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IDirectorateRepository>();
        var service = new DirectorateService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<DirectorateValidationException>(() =>
            service.CreateAsync(""));
    }

    [Fact]
    public async Task CreateAsync_TooLongName_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IDirectorateRepository>();
        var service = new DirectorateService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<DirectorateValidationException>(() =>
            service.CreateAsync(new string('a', 251)));
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        var repo = new Mock<IDirectorateRepository>();
        repo.Setup(r => r.TitleExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new DirectorateService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<DirectorateConflictException>(() =>
            service.CreateAsync("Existing"));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDirectorates()
    {
        // Arrange
        var directorates = new List<Directorate>
        {
            Directorate.Rehydrate(1, "Dir1", true),
            Directorate.Rehydrate(2, "Dir2", true)
        };
        var repo = new Mock<IDirectorateRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Directorate>)directorates);

        var service = new DirectorateService(repo.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var directorate = Directorate.Rehydrate(1, "Old", true);
        var repo = new Mock<IDirectorateRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directorate);
        repo.Setup(r => r.TitleExistsAsync(It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Directorate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new DirectorateService(repo.Object);

        // Act
        var result = await service.UpdateAsync(1, "New Name", true);

        // Assert
        result.Should().Be(1);
        directorate.Title.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IDirectorateRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Directorate?)null);

        var service = new DirectorateService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<DirectorateNotFoundException>(() =>
            service.UpdateAsync(99, "New", true));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        var directorate = Directorate.Rehydrate(1, "Old", true);
        var repo = new Mock<IDirectorateRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directorate);
        repo.Setup(r => r.TitleExistsAsync(It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new DirectorateService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<DirectorateConflictException>(() =>
            service.UpdateAsync(1, "Existing", true));
    }

    [Fact]
    public async Task DeactivateAsync_ValidInput_Deactivates()
    {
        // Arrange
        var directorate = Directorate.Rehydrate(1, "Test", true);
        var repo = new Mock<IDirectorateRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directorate);
        repo.Setup(r => r.GetDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DirectorateDependencies(0, 0, 0));
        repo.Setup(r => r.DeactivateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new DirectorateService(repo.Object);

        // Act
        await service.DeactivateAsync(1);

        // Assert
        directorate.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IDirectorateRepository>();
        var service = new DirectorateService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<DirectorateValidationException>(() =>
            service.DeactivateAsync(0));
    }

    [Fact]
    public async Task DeactivateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IDirectorateRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Directorate?)null);

        var service = new DirectorateService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<DirectorateNotFoundException>(() =>
            service.DeactivateAsync(99));
    }

    [Fact]
    public async Task DeactivateAsync_HasDependencies_ThrowsHasDependenciesException()
    {
        // Arrange
        var directorate = Directorate.Rehydrate(1, "Test", true);
        var repo = new Mock<IDirectorateRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directorate);
        repo.Setup(r => r.GetDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DirectorateDependencies(1, 0, 0));

        var service = new DirectorateService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<DirectorateHasDependenciesException>(() =>
            service.DeactivateAsync(1));
    }
}
