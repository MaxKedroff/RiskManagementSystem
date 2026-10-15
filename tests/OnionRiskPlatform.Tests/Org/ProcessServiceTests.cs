using Application.Abstractions;
using Application.Org.Processes;
using Domain.Org;

namespace OnionRiskPlatform.Tests.Org;

public class ProcessServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var repo = new Mock<IProcessRepository>();
        repo.Setup(r => r.DirectorateIsActiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.UserExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.UserHasRoleAsync(1, "OWNER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.CreateAsync(It.IsAny<BusinessProcess>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var service = new ProcessService(repo.Object);

        // Act
        var result = await service.CreateAsync(1, "Test Process", 1, "Description");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IProcessRepository>();
        var service = new ProcessService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProcessValidationException>(() =>
            service.CreateAsync(1, "", 1, null));
    }

    [Fact]
    public async Task CreateAsync_InvalidDirectorateId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IProcessRepository>();
        var service = new ProcessService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProcessValidationException>(() =>
            service.CreateAsync(0, "Process", 1, null));
    }

    [Fact]
    public async Task CreateAsync_InactiveDirectorate_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IProcessRepository>();
        repo.Setup(r => r.DirectorateIsActiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new ProcessService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProcessValidationException>(() =>
            service.CreateAsync(1, "Process", 1, null));
    }

    [Fact]
    public async Task CreateAsync_UserNotFound_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IProcessRepository>();
        repo.Setup(r => r.DirectorateIsActiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.UserExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new ProcessService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProcessValidationException>(() =>
            service.CreateAsync(1, "Process", 1, null));
    }

    [Fact]
    public async Task CreateAsync_UserNotOwner_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IProcessRepository>();
        repo.Setup(r => r.DirectorateIsActiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.UserExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.UserHasRoleAsync(1, "OWNER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new ProcessService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProcessValidationException>(() =>
            service.CreateAsync(1, "Process", 1, null));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllProcesses()
    {
        // Arrange
        var processes = new List<BusinessProcess>
        {
            BusinessProcess.Rehydrate(1, 1, "P1", 1, "Desc", true),
            BusinessProcess.Rehydrate(2, 1, "P2", 1, null, true)
        };
        var repo = new Mock<IProcessRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BusinessProcess>)processes);

        var service = new ProcessService(repo.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsProcess()
    {
        // Arrange
        var process = BusinessProcess.Rehydrate(1, 1, "Test", 1, "Desc", true);
        var repo = new Mock<IProcessRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        var service = new ProcessService(repo.Object);

        // Act
        var result = await service.GetByIdAsync(1);

        // Assert
        result.Should().Be(process);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IProcessRepository>();
        var service = new ProcessService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProcessValidationException>(() =>
            service.GetByIdAsync(0));
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IProcessRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusinessProcess?)null);

        var service = new ProcessService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProcessNotFoundException>(() =>
            service.GetByIdAsync(99));
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var process = BusinessProcess.Rehydrate(1, 1, "Old", 1, "Desc", true);
        var repo = new Mock<IProcessRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);
        repo.Setup(r => r.DirectorateIsActiveAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.UserExistsAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.UserHasRoleAsync(2, "OWNER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.UpdateAsync(It.IsAny<ProcessUpdate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new ProcessService(repo.Object);

        // Act
        var result = await service.UpdateAsync(1, 2, "New", 2, "New Desc", true);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_NoFields_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IProcessRepository>();
        var service = new ProcessService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProcessValidationException>(() =>
            service.UpdateAsync(1, null, null, null, null, null));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IProcessRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusinessProcess?)null);

        var service = new ProcessService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProcessNotFoundException>(() =>
            service.UpdateAsync(99, 1, "New", 1, "Desc", true));
    }

    [Fact]
    public async Task UpdateAsync_TooLongDescription_ThrowsValidationException()
    {
        // Arrange
        var process = BusinessProcess.Rehydrate(1, 1, "Test", 1, "Desc", true);
        var repo = new Mock<IProcessRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);
        repo.Setup(r => r.UpdateAsync(It.IsAny<ProcessUpdate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new ProcessService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProcessValidationException>(() =>
            service.UpdateAsync(1, 1, "Test", 1, new string('a', 1001), true));
    }

    [Fact]
    public async Task DeactivateAsync_ValidInput_Deactivates()
    {
        // Arrange
        var process = BusinessProcess.Rehydrate(1, 1, "Test", 1, "Desc", true);
        var repo = new Mock<IProcessRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);
        repo.Setup(r => r.GetDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessDependencies { RiskProcesses = 0, Responsibilities = 0, OwnedProcesses = 0 });
        repo.Setup(r => r.DeactivateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new ProcessService(repo.Object);

        // Act
        await service.DeactivateAsync(1);

        // Assert - DeactivateAsync was called on the repo
        repo.Verify(r => r.DeactivateAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IProcessRepository>();
        var service = new ProcessService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProcessValidationException>(() =>
            service.DeactivateAsync(0));
    }

    [Fact]
    public async Task DeactivateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IProcessRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusinessProcess?)null);

        var service = new ProcessService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProcessNotFoundException>(() =>
            service.DeactivateAsync(99));
    }

    [Fact]
    public async Task DeactivateAsync_HasDependencies_ThrowsHasDependenciesException()
    {
        // Arrange
        var process = BusinessProcess.Rehydrate(1, 1, "Test", 1, "Desc", true);
        var repo = new Mock<IProcessRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);
        repo.Setup(r => r.GetDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessDependencies { RiskProcesses = 0, Responsibilities = 1, OwnedProcesses = 0 });

        var service = new ProcessService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProcessHasDependenciesException>(() =>
            service.DeactivateAsync(1));
    }
}
