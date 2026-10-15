using Application.Abstractions;
using Application.Org.Users;
using Domain.Roles;

namespace OnionRiskPlatform.Tests.Org;

public class UserServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.RoleExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var passwordGen = new Mock<ITemporaryPasswordGenerator>();
        passwordGen.Setup(p => p.Generate()).Returns("TempPass123");

        var hashService = new Mock<IPasswordHashService>();
        hashService.Setup(h => h.Hash("TempPass123")).Returns("hashed");

        var service = new UserService(repo.Object, hashService.Object, passwordGen.Object);

        // Act
        var result = await service.CreateAsync("John Doe", "john@test.com", new[] { 1 }, null, null, null);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task CreateAsync_EmptyFio_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IUserRepository>();
        var passwordGen = new Mock<ITemporaryPasswordGenerator>();
        var hashService = new Mock<IPasswordHashService>();
        var service = new UserService(repo.Object, hashService.Object, passwordGen.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserValidationException>(() =>
            service.CreateAsync("", "john@test.com", new[] { 1 }, null, null, null));
    }

    [Fact]
    public async Task CreateAsync_InvalidEmail_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IUserRepository>();
        var passwordGen = new Mock<ITemporaryPasswordGenerator>();
        var hashService = new Mock<IPasswordHashService>();
        var service = new UserService(repo.Object, hashService.Object, passwordGen.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserValidationException>(() =>
            service.CreateAsync("John", "invalid-email", new[] { 1 }, null, null, null));
    }

    [Fact]
    public async Task CreateAsync_EmptyRoles_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IUserRepository>();
        var passwordGen = new Mock<ITemporaryPasswordGenerator>();
        var hashService = new Mock<IPasswordHashService>();
        var service = new UserService(repo.Object, hashService.Object, passwordGen.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserValidationException>(() =>
            service.CreateAsync("John", "john@test.com", Array.Empty<int>(), null, null, null));
    }

    [Fact]
    public async Task CreateAsync_RoleNotFound_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.RoleExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var passwordGen = new Mock<ITemporaryPasswordGenerator>();
        var hashService = new Mock<IPasswordHashService>();
        var service = new UserService(repo.Object, hashService.Object, passwordGen.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserValidationException>(() =>
            service.CreateAsync("John", "john@test.com", new[] { 1 }, null, null, null));
    }

    [Fact]
    public async Task CreateAsync_InvalidResponsibilityType_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.RoleExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var passwordGen = new Mock<ITemporaryPasswordGenerator>();
        var hashService = new Mock<IPasswordHashService>();
        var service = new UserService(repo.Object, hashService.Object, passwordGen.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserValidationException>(() =>
            service.CreateAsync("John", "john@test.com", new[] { 1 }, null, null, "INVALID"));
    }

    [Fact]
    public async Task CreateAsync_PersonNotFound_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.RoleExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.PersonExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var passwordGen = new Mock<ITemporaryPasswordGenerator>();
        var hashService = new Mock<IPasswordHashService>();
        var service = new UserService(repo.Object, hashService.Object, passwordGen.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserValidationException>(() =>
            service.CreateAsync("John", "john@test.com", new[] { 1 }, 1, null, null));
    }

    [Fact]
    public async Task CreateAsync_DirectorateInactive_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.RoleExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.DirectorateIsActiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var passwordGen = new Mock<ITemporaryPasswordGenerator>();
        var hashService = new Mock<IPasswordHashService>();
        var service = new UserService(repo.Object, hashService.Object, passwordGen.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserValidationException>(() =>
            service.CreateAsync("John", "john@test.com", new[] { 1 }, null, 1, null));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            User.Rehydrate(1, "John", "john@test.com", "hash", true, null, null, null),
            User.Rehydrate(2, "Jane", "jane@test.com", "hash", true, 1, 1, "CEO")
        };
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<User>)users);

        var service = new UserService(repo.Object, new Mock<IPasswordHashService>().Object, new Mock<ITemporaryPasswordGenerator>().Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsUserWithRoles()
    {
        // Arrange
        var user = User.Rehydrate(1, "John", "john@test.com", "hash", true, null, null, null);
        var roles = new List<Role> { Role.Rehydrate(1, "ADMIN", "Админ", true) };
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        repo.Setup(r => r.GetRolesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Role>)roles);

        var service = new UserService(repo.Object, new Mock<IPasswordHashService>().Object, new Mock<ITemporaryPasswordGenerator>().Object);

        // Act
        var (u, r) = await service.GetByIdAsync(1);

        // Assert
        u.Should().Be(user);
        r.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IUserRepository>();
        var service = new UserService(repo.Object, new Mock<IPasswordHashService>().Object, new Mock<ITemporaryPasswordGenerator>().Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserValidationException>(() =>
            service.GetByIdAsync(0));
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new UserService(repo.Object, new Mock<IPasswordHashService>().Object, new Mock<ITemporaryPasswordGenerator>().Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            service.GetByIdAsync(99));
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var user = User.Rehydrate(1, "Old", "old@test.com", "hash", true, null, null, null);
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        repo.Setup(r => r.RoleExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.IsLastAdminAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.UpdateAsync(It.IsAny<User.UserUpdate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new UserService(repo.Object, new Mock<IPasswordHashService>().Object, new Mock<ITemporaryPasswordGenerator>().Object);

        // Act
        var result = await service.UpdateAsync(1, "New Name", new[] { 1 }, true);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_NoFields_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IUserRepository>();
        var service = new UserService(repo.Object, new Mock<IPasswordHashService>().Object, new Mock<ITemporaryPasswordGenerator>().Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserValidationException>(() =>
            service.UpdateAsync(1, null, null, null));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new UserService(repo.Object, new Mock<IPasswordHashService>().Object, new Mock<ITemporaryPasswordGenerator>().Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            service.UpdateAsync(99, "Name", new[] { 1 }, true));
    }

    [Fact]
    public async Task UpdateAsync_EmptyRoles_ThrowsValidationException()
    {
        // Arrange
        var user = User.Rehydrate(1, "Test", "test@test.com", "hash", true, null, null, null);
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = new UserService(repo.Object, new Mock<IPasswordHashService>().Object, new Mock<ITemporaryPasswordGenerator>().Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserValidationException>(() =>
            service.UpdateAsync(1, "Name", Array.Empty<int>(), true));
    }

    [Fact]
    public async Task UpdateAsync_LastAdmin_ThrowsUserLastAdminException()
    {
        // Arrange
        var user = User.Rehydrate(1, "Admin", "admin@test.com", "hash", true, null, null, null);
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        repo.Setup(r => r.IsLastAdminAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new UserService(repo.Object, new Mock<IPasswordHashService>().Object, new Mock<ITemporaryPasswordGenerator>().Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserLastAdminException>(() =>
            service.UpdateAsync(1, null, null, false));
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidInput_ReturnsNewPassword()
    {
        // Arrange
        var user = User.Rehydrate(1, "Test", "test@test.com", "hash", true, null, null, null);
        var newPassword = "NewTemp123";
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        repo.Setup(r => r.UpdatePasswordHashAsync(1, "hashed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var passwordGen = new Mock<ITemporaryPasswordGenerator>();
        passwordGen.Setup(p => p.Generate()).Returns(newPassword);

        var hashService = new Mock<IPasswordHashService>();
        hashService.Setup(h => h.Hash(newPassword)).Returns("hashed");

        var service = new UserService(repo.Object, hashService.Object, passwordGen.Object);

        // Act
        var result = await service.ResetPasswordAsync(1);

        // Assert
        result.Should().Be(newPassword);
    }

    [Fact]
    public async Task ResetPasswordAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IUserRepository>();
        var service = new UserService(repo.Object, new Mock<IPasswordHashService>().Object, new Mock<ITemporaryPasswordGenerator>().Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserValidationException>(() =>
            service.ResetPasswordAsync(0));
    }

    [Fact]
    public async Task ResetPasswordAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new UserService(repo.Object, new Mock<IPasswordHashService>().Object, new Mock<ITemporaryPasswordGenerator>().Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            service.ResetPasswordAsync(99));
    }
}
