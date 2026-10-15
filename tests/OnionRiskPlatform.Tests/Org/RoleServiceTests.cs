using Application.Abstractions;
using Application.Org.Roles;
using Domain.Roles;

namespace OnionRiskPlatform.Tests.Org;

public class RoleServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsAllRoles()
    {
        // Arrange
        var roles = new List<Role>
        {
            Role.Rehydrate(1, "ADMIN", "Админ", true),
            Role.Rehydrate(2, "CEO", "Директор", true)
        };
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Role>)roles);

        var service = new RoleService(repo.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRightsAsync_ValidId_ReturnsRights()
    {
        // Arrange
        var role = Role.Rehydrate(1, "ADMIN", "Админ", true);
        var rights = new List<Right>
        {
            Right.Rehydrate(1, "view_risk", "Просмотр рисков"),
            Right.Rehydrate(2, "create_components", "Создание")
        };
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        repo.Setup(r => r.GetRightsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Right>)rights);

        var service = new RoleService(repo.Object);

        // Act
        var result = await service.GetRightsAsync(1);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRightsAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IRoleRepository>();
        var service = new RoleService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RoleValidationException>(() =>
            service.GetRightsAsync(0));
    }

    [Fact]
    public async Task GetRightsAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var service = new RoleService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RoleNotFoundException>(() =>
            service.GetRightsAsync(99));
    }

    [Fact]
    public async Task GetAllRightsAsync_ReturnsAllRights()
    {
        // Arrange
        var rights = new List<Right>
        {
            Right.Rehydrate(1, "view_risk", "Просмотр"),
            Right.Rehydrate(2, "edit_components", "Редактирование")
        };
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetAllRightsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Right>)rights);

        var service = new RoleService(repo.Object);

        // Act
        var result = await service.GetAllRightsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateRightsAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var role = Role.Rehydrate(1, "ADMIN", "Админ", true);
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        repo.Setup(r => r.RightExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.RightExistsAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.IsAdminRoleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.UpdateRightsAsync(1, It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new RoleService(repo.Object);

        // Act
        var result = await service.UpdateRightsAsync(1, new[] { 1, 2 });

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task UpdateRightsAsync_NullRights_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IRoleRepository>();
        var service = new RoleService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RoleValidationException>(() =>
            service.UpdateRightsAsync(1, null!));
    }

    [Fact]
    public async Task UpdateRightsAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IRoleRepository>();
        var service = new RoleService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RoleValidationException>(() =>
            service.UpdateRightsAsync(0, new[] { 1 }));
    }

    [Fact]
    public async Task UpdateRightsAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var service = new RoleService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RoleNotFoundException>(() =>
            service.UpdateRightsAsync(99, new[] { 1 }));
    }

    [Fact]
    public async Task UpdateRightsAsync_RightNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var role = Role.Rehydrate(1, "ADMIN", "Админ", true);
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        repo.Setup(r => r.RightExistsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new RoleService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RightNotFoundException>(() =>
            service.UpdateRightsAsync(1, new[] { 99 }));
    }

    [Fact]
    public async Task UpdateRightsAsync_AdminRoleRemovingAdminRight_ThrowsValidationException()
    {
        // Arrange
        var role = Role.Rehydrate(1, "ADMIN", "Админ", true);
        var adminRight = Right.Rehydrate(1, "ADMIN", "Admin right");
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        repo.Setup(r => r.RightExistsAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.IsAdminRoleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.RoleHasRightAsync(1, "ADMIN", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.GetAllRightsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Right> { adminRight });

        var service = new RoleService(repo.Object);

        // Act & Assert — trying to update ADMIN role without the admin right
        await Assert.ThrowsAsync<RoleValidationException>(() =>
            service.UpdateRightsAsync(1, new[] { 2 }));
    }

    [Fact]
    public async Task UpdateRightsAsync_AdminRoleKeepingAdminRight_ReturnsId()
    {
        // Arrange
        var role = Role.Rehydrate(1, "ADMIN", "Админ", true);
        var adminRight = Right.Rehydrate(1, "ADMIN", "Admin right");
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        repo.Setup(r => r.RightExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.RightExistsAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.IsAdminRoleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.RoleHasRightAsync(1, "ADMIN", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.GetAllRightsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Right> { adminRight });
        repo.Setup(r => r.UpdateRightsAsync(1, It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new RoleService(repo.Object);

        // Act
        var result = await service.UpdateRightsAsync(1, new[] { 1, 2 });

        // Assert
        result.Should().Be(1);
    }
}
