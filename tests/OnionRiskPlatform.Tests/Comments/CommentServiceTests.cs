using Application.Abstractions;
using Application.Comments;
using Domain.Risk;
using Moq;

namespace OnionRiskPlatform.Tests.Comments;

public class CommentServiceTests
{
    private readonly Mock<ICommentRepository> _commentRepository;
    private readonly Mock<IUserPermissionService> _permissionService;
    private readonly CommentService _service;

    public CommentServiceTests()
    {
        _commentRepository = new Mock<ICommentRepository>();
        _permissionService = new Mock<IUserPermissionService>();

        _service = new CommentService(
            _commentRepository.Object,
            _permissionService.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidInput_CreatesComment()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(true);
        _commentRepository.Setup(r => r.CreateAsync(1, "Test comment", "Risk", 42, CancellationToken.None))
            .ReturnsAsync(100);

        var request = new CreateCommentRequest(
            CommentEntityType: "Risk",
            CommentEntityId: 42,
            CommentText: "Test comment");

        // Act
        var result = await _service.CreateAsync(1, request);

        // Assert
        result.Should().Be(100);
        _commentRepository.Verify(r => r.CreateAsync(1, "Test comment", "Risk", 42, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmptyText_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(true);

        var request = new CreateCommentRequest("Risk", 42, "");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CommentValidationException>(() =>
            _service.CreateAsync(1, request));
        ex.Message.Should().Contain("пустым");
    }

    [Fact]
    public async Task CreateAsync_WhitespaceText_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(true);

        var request = new CreateCommentRequest("Risk", 42, "   ");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CommentValidationException>(() =>
            _service.CreateAsync(1, request));
        ex.Message.Should().Contain("пустым");
    }

    [Fact]
    public async Task CreateAsync_WithoutPermission_ThrowsPermissionException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(false);

        var request = new CreateCommentRequest("Risk", 42, "Test comment");

        // Act & Assert
        await Assert.ThrowsAsync<CommentPermissionException>(() =>
            _service.CreateAsync(1, request));
    }

    [Fact]
    public async Task CreateAsync_TextTrimmed()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(true);
        _commentRepository.Setup(r => r.CreateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(101);

        var request = new CreateCommentRequest("Risk", 42, "  Trimmed text  ");

        // Act
        var result = await _service.CreateAsync(1, request);

        // Assert
        result.Should().Be(101);
        _commentRepository.Verify(r => r.CreateAsync(
            1,
            "Trimmed text",
            "Risk",
            42,
            CancellationToken.None), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidInput_UpdatesComment()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(true);

        var comment = RiskComment.Rehydrate(1, 1, "Old text", "Risk", 42, true,
            DateTimeOffset.UtcNow, null);
        _commentRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(comment);
        _commentRepository.Setup(r => r.EditAsync(1, "New text", CancellationToken.None))
            .ReturnsAsync(true);

        var request = new UpdateCommentRequest("New text");

        // Act
        var result = await _service.UpdateAsync(1, 1, request);

        // Assert
        result.Should().Be(1);
        _commentRepository.Verify(r => r.EditAsync(1, "New text", CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotOwner_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(true);

        var comment = RiskComment.Rehydrate(1, 99, "Old text", "Risk", 42, true,
            DateTimeOffset.UtcNow, null);
        _commentRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(comment);

        var request = new UpdateCommentRequest("New text");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CommentValidationException>(() =>
            _service.UpdateAsync(1, 1, request));
        ex.Message.Should().Contain("нет прав на редактирование");
    }

    [Fact]
    public async Task UpdateAsync_DeletedComment_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(true);

        var comment = RiskComment.Rehydrate(1, 1, "Old text", "Risk", 42, false,
            DateTimeOffset.UtcNow, null);
        _commentRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(comment);

        var request = new UpdateCommentRequest("New text");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CommentValidationException>(() =>
            _service.UpdateAsync(1, 1, request));
        ex.Message.Should().Contain("удалённый");
    }

    [Fact]
    public async Task UpdateAsync_EmptyText_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(true);

        var comment = RiskComment.Rehydrate(1, 1, "Old text", "Risk", 42, true,
            DateTimeOffset.UtcNow, null);
        _commentRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(comment);

        var request = new UpdateCommentRequest("");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CommentValidationException>(() =>
            _service.UpdateAsync(1, 1, request));
        ex.Message.Should().Contain("пустым");
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(true);

        _commentRepository.Setup(r => r.GetByIdAsync(99, CancellationToken.None))
            .ReturnsAsync((RiskComment?)null);

        var request = new UpdateCommentRequest("New text");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CommentNotFoundException>(() =>
            _service.UpdateAsync(99, 1, request));
        ex.Message.Should().Contain("99");
    }

    [Fact]
    public async Task UpdateAsync_WithoutPermission_ThrowsPermissionException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(false);

        var request = new UpdateCommentRequest("New text");

        // Act & Assert
        await Assert.ThrowsAsync<CommentPermissionException>(() =>
            _service.UpdateAsync(1, 1, request));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ValidInput_SoftDeletesComment()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(true);

        var comment = RiskComment.Rehydrate(1, 1, "Test", "Risk", 42, true,
            DateTimeOffset.UtcNow, null);
        _commentRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(comment);
        _commentRepository.Setup(r => r.SoftDeleteAsync(1, CancellationToken.None))
            .ReturnsAsync(true);

        // Act
        await _service.DeleteAsync(1, 1);

        // Assert
        _commentRepository.Verify(r => r.SoftDeleteAsync(1, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(true);

        var comment = RiskComment.Rehydrate(1, 99, "Test", "Risk", 42, true,
            DateTimeOffset.UtcNow, null);
        _commentRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(comment);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CommentValidationException>(() =>
            _service.DeleteAsync(1, 1));
        ex.Message.Should().Contain("нет прав на удаление");
    }

    [Fact]
    public async Task DeleteAsync_AlreadyDeleted_ThrowsValidationException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(true);

        var comment = RiskComment.Rehydrate(1, 1, "Test", "Risk", 42, false,
            DateTimeOffset.UtcNow, null);
        _commentRepository.Setup(r => r.GetByIdAsync(1, CancellationToken.None))
            .ReturnsAsync(comment);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CommentValidationException>(() =>
            _service.DeleteAsync(1, 1));
        ex.Message.Should().Contain("уже удалён");
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(true);

        _commentRepository.Setup(r => r.GetByIdAsync(99, CancellationToken.None))
            .ReturnsAsync((RiskComment?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CommentNotFoundException>(() =>
            _service.DeleteAsync(99, 1));
        ex.Message.Should().Contain("99");
    }

    [Fact]
    public async Task DeleteAsync_WithoutPermission_ThrowsPermissionException()
    {
        // Arrange
        _permissionService.Setup(p => p.HasRightAsync(1, "comment_components", CancellationToken.None))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<CommentPermissionException>(() =>
            _service.DeleteAsync(1, 1));
    }

    #endregion

    #region GetAllByEntityAsync Tests

    [Fact]
    public async Task GetAllByEntityAsync_ReturnsComments()
    {
        // Arrange
        var comments = new List<RiskComment>
        {
            RiskComment.Rehydrate(1, 1, "First", "Risk", 42, true, DateTimeOffset.UtcNow, null),
            RiskComment.Rehydrate(2, 2, "Second", "Risk", 42, true, DateTimeOffset.UtcNow.AddDays(1), null)
        };
        _commentRepository.Setup(r => r.GetAllByEntityAsync("Risk", 42, CancellationToken.None))
            .ReturnsAsync(comments);

        // Act
        var result = await _service.GetAllByEntityAsync("Risk", 42);

        // Assert
        result.Should().HaveCount(2);
        result[0].CommentId.Should().Be(1);
        result[0].Text.Should().Be("First");
        result[1].CommentId.Should().Be(2);
        result[1].Text.Should().Be("Second");
    }

    [Fact]
    public async Task GetAllByEntityAsync_EmptyReturnsEmptyList()
    {
        // Arrange
        _commentRepository.Setup(r => r.GetAllByEntityAsync("Risk", 99, CancellationToken.None))
            .ReturnsAsync(new List<RiskComment>());

        // Act
        var result = await _service.GetAllByEntityAsync("Risk", 99);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
