using Application.Abstractions;
using Application.Import.FileUploads;
using Domain.Import;

namespace OnionRiskPlatform.Tests.Import;

public class FileUploadServiceTests
{
    [Fact]
    public async Task UploadAsync_ValidInput_ReturnsId()
    {
        // Arrange
        var repo = new Mock<IFileUploadRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<FileUpload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var service = new FileUploadService(repo.Object);

        // Act
        var result = await service.UploadAsync("Test File", 1);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task UploadAsync_EmptyTitle_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IFileUploadRepository>();
        var service = new FileUploadService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<FileUploadValidationException>(() =>
            service.UploadAsync("", 1));
    }

    [Fact]
    public async Task UploadAsync_InvalidPersonId_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IFileUploadRepository>();
        var service = new FileUploadService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<FileUploadValidationException>(() =>
            service.UploadAsync("File", 0));
    }

    [Fact]
    public async Task ConfirmAsync_ValidInput_ReturnsResponse()
    {
        // Arrange
        var upload = FileUpload.Rehydrate(1, "File", ImportStatus.Uploaded, 1, 0, 0, 0, "", null);
        var repo = new Mock<IFileUploadRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(upload);
        repo.Setup(r => r.CompleteAsync(1, 0, 0, 0, "", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new FileUploadService(repo.Object);

        // Act
        var result = await service.ConfirmAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.FileUploadId.Should().Be(1);
    }

    [Fact]
    public async Task ConfirmAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repo = new Mock<IFileUploadRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileUpload?)null);

        var service = new FileUploadService(repo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<FileUploadNotFoundException>(() =>
            service.ConfirmAsync(99));
    }
}
