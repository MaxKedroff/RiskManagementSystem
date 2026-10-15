using Application.Abstractions;
using Application.Risk.Categories;
using Domain.Risk;

namespace OnionRiskPlatform.Tests.Risk;

public class RiskCategoryServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var repo = new Mock<IRiskCategoryRepository>();
        repo.Setup(r => r.NameExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.CreateAsync(It.IsAny<RiskCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var service = new RiskCategoryService(repo.Object);

        // Act
        var result = await service.CreateAsync("Financial");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IRiskCategoryRepository>();
        var service = new RiskCategoryService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RiskCategoryValidationException>(() =>
            service.CreateAsync(""));
    }

    [Fact]
    public async Task CreateAsync_TooLongName_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IRiskCategoryRepository>();
        var service = new RiskCategoryService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RiskCategoryValidationException>(() =>
            service.CreateAsync(new string('a', 251)));
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        var repo = new Mock<IRiskCategoryRepository>();
        repo.Setup(r => r.NameExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new RiskCategoryService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RiskCategoryConflictException>(() =>
            service.CreateAsync("Existing"));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCategories()
    {
        // Arrange
        var categories = new List<RiskCategory>
        {
            RiskCategory.Rehydrate(1, "Financial", true),
            RiskCategory.Rehydrate(2, "Operational", true)
        };
        var repo = new Mock<IRiskCategoryRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<RiskCategory>)categories);

        var service = new RiskCategoryService(repo.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsCategory()
    {
        // Arrange
        var category = RiskCategory.Rehydrate(1, "Financial", true);
        var repo = new Mock<IRiskCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var service = new RiskCategoryService(repo.Object);

        // Act
        var result = await service.GetByIdAsync(1);

        // Assert
        result.Should().Be(category);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IRiskCategoryRepository>();
        var service = new RiskCategoryService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RiskCategoryValidationException>(() =>
            service.GetByIdAsync(0));
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IRiskCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RiskCategory?)null);

        var service = new RiskCategoryService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RiskCategoryNotFoundException>(() =>
            service.GetByIdAsync(99));
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var category = RiskCategory.Rehydrate(1, "Old", true);
        var repo = new Mock<IRiskCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        repo.Setup(r => r.NameExistsAsync(It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.UpdateAsync(It.IsAny<RiskCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new RiskCategoryService(repo.Object);

        // Act
        var result = await service.UpdateAsync(1, "New Name", true);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_NoFields_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IRiskCategoryRepository>();
        var service = new RiskCategoryService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RiskCategoryValidationException>(() =>
            service.UpdateAsync(1, null, null));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IRiskCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RiskCategory?)null);

        var service = new RiskCategoryService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RiskCategoryNotFoundException>(() =>
            service.UpdateAsync(99, "New", true));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        var category = RiskCategory.Rehydrate(1, "Old", true);
        var repo = new Mock<IRiskCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        repo.Setup(r => r.NameExistsAsync(It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new RiskCategoryService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RiskCategoryConflictException>(() =>
            service.UpdateAsync(1, "Existing", true));
    }

    [Fact]
    public async Task DeactivateAsync_ValidInput_Deactivates()
    {
        // Arrange
        var category = RiskCategory.Rehydrate(1, "Test", true);
        var repo = new Mock<IRiskCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        repo.Setup(r => r.GetDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RiskCategoryDependencies { Risks = 0 });
        repo.Setup(r => r.DeactivateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new RiskCategoryService(repo.Object);

        // Act
        await service.DeactivateAsync(1);

        // Assert - DeactivateAsync was called on the repo
        repo.Verify(r => r.DeactivateAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IRiskCategoryRepository>();
        var service = new RiskCategoryService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RiskCategoryValidationException>(() =>
            service.DeactivateAsync(0));
    }

    [Fact]
    public async Task DeactivateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IRiskCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RiskCategory?)null);

        var service = new RiskCategoryService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RiskCategoryNotFoundException>(() =>
            service.DeactivateAsync(99));
    }

    [Fact]
    public async Task DeactivateAsync_HasDependencies_ThrowsHasDependenciesException()
    {
        // Arrange
        var category = RiskCategory.Rehydrate(1, "Test", true);
        var repo = new Mock<IRiskCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        repo.Setup(r => r.GetDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RiskCategoryDependencies { Risks = 3 });

        var service = new RiskCategoryService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RiskCategoryHasDependenciesException>(() =>
            service.DeactivateAsync(1));
    }
}
