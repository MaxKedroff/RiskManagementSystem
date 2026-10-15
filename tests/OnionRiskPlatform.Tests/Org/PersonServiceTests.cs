using Application.Abstractions;
using Application.Org.Persons;
using Domain.Org;

namespace OnionRiskPlatform.Tests.Org;

public class PersonServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var repo = new Mock<IPersonRepository>();
        repo.Setup(r => r.UserExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.DirectorateIsActiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.CreateAsync(It.IsAny<Person>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var service = new PersonService(repo.Object);

        // Act
        var result = await service.CreateAsync(1, 1, "Developer");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task CreateAsync_InvalidUserId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IPersonRepository>();
        var service = new PersonService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<PersonValidationException>(() =>
            service.CreateAsync(0, 1, "Developer"));
    }

    [Fact]
    public async Task CreateAsync_EmptyPosition_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IPersonRepository>();
        var service = new PersonService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<PersonValidationException>(() =>
            service.CreateAsync(1, 1, ""));
    }

    [Fact]
    public async Task CreateAsync_UserNotFound_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IPersonRepository>();
        repo.Setup(r => r.UserExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new PersonService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<PersonValidationException>(() =>
            service.CreateAsync(1, 1, "Developer"));
    }

    [Fact]
    public async Task CreateAsync_DirectorateInactive_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IPersonRepository>();
        repo.Setup(r => r.UserExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.DirectorateIsActiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new PersonService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<PersonValidationException>(() =>
            service.CreateAsync(1, 1, "Developer"));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllPersons()
    {
        // Arrange
        var persons = new List<Person>
        {
            Person.Rehydrate(1, 1, 1, "Dev", true),
            Person.Rehydrate(2, 2, 1, "Manager", true)
        };
        var repo = new Mock<IPersonRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Person>)persons);

        var service = new PersonService(repo.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var person = Person.Rehydrate(1, 1, 1, "Dev", true);
        var repo = new Mock<IPersonRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);
        repo.Setup(r => r.DirectorateIsActiveAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Person.PersonUpdate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new PersonService(repo.Object);

        // Act
        var result = await service.UpdateAsync(1, 2, "Senior Dev", true);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_NoFields_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IPersonRepository>();
        var service = new PersonService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<PersonValidationException>(() =>
            service.UpdateAsync(1, null, null, null));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IPersonRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Person?)null);

        var service = new PersonService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<PersonNotFoundException>(() =>
            service.UpdateAsync(99, 1, "Dev", true));
    }

    [Fact]
    public async Task UpdateAsync_InvalidDirectorate_ThrowsValidationException()
    {
        // Arrange
        var person = Person.Rehydrate(1, 1, 1, "Dev", true);
        var repo = new Mock<IPersonRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);
        repo.Setup(r => r.DirectorateIsActiveAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new PersonService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<PersonValidationException>(() =>
            service.UpdateAsync(1, 0, "Dev", true));
    }

    [Fact]
    public async Task DeactivateAsync_ValidInput_Deactivates()
    {
        // Arrange
        var person = Person.Rehydrate(1, 1, 1, "Dev", true);
        var repo = new Mock<IPersonRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);
        repo.Setup(r => r.GetDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersonDependencies(0, 0, 0));
        repo.Setup(r => r.DeactivateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new PersonService(repo.Object);

        // Act
        await service.DeactivateAsync(1);

        // Assert - DeactivateAsync was called on the repo
        repo.Verify(r => r.DeactivateAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IPersonRepository>();
        var service = new PersonService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<PersonValidationException>(() =>
            service.DeactivateAsync(0));
    }

    [Fact]
    public async Task DeactivateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IPersonRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Person?)null);

        var service = new PersonService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<PersonNotFoundException>(() =>
            service.DeactivateAsync(99));
    }

    [Fact]
    public async Task DeactivateAsync_HasDependencies_ThrowsHasDependenciesException()
    {
        // Arrange
        var person = Person.Rehydrate(1, 1, 1, "Dev", true);
        var repo = new Mock<IPersonRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);
        repo.Setup(r => r.GetDependenciesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersonDependencies(1, 0, 0));

        var service = new PersonService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<PersonHasDependenciesException>(() =>
            service.DeactivateAsync(1));
    }
}
