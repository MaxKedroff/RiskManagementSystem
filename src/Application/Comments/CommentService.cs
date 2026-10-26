using Application.Abstractions;
using Domain.Risk;

namespace Application.Comments;

public sealed class CommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IUserPermissionService _permissionService;

    public CommentService(
        ICommentRepository commentRepository,
        IUserPermissionService permissionService)
    {
        _commentRepository = commentRepository;
        _permissionService = permissionService;
    }

    // POST /v1/comments
    public async Task<int> CreateAsync(
        int userId,
        CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "comment_components", cancellationToken))
            throw new CommentPermissionException("Отсутствует право comment_components.");

        // Validate text is not empty
        var normalizedText = ValidateText(request.CommentText);

        var commentId = await _commentRepository.CreateAsync(
            userId,
            normalizedText,
            request.CommentEntityType,
            request.CommentEntityId,
            cancellationToken);

        return commentId;
    }

    // PUT /v1/comments/{id}
    public async Task<int> UpdateAsync(
        int commentId,
        int userId,
        UpdateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "comment_components", cancellationToken))
            throw new CommentPermissionException("Отсутствует право comment_components.");

        var comment = await _commentRepository.GetByIdAsync(commentId, cancellationToken)
            ?? throw new CommentNotFoundException(commentId);

        // Check authorship
        if (comment.AuthorId != userId)
            throw new CommentValidationException("У вас нет прав на редактирование этого комментария.");

        // Check not deleted
        if (!comment.IsActive)
            throw new CommentValidationException("Нельзя редактировать удалённый комментарий.");

        // Validate text
        var normalizedText = ValidateText(request.CommentText);

        // Update domain entity
        comment.Edit(normalizedText);

        var updated = await _commentRepository.EditAsync(commentId, normalizedText, cancellationToken);
        if (!updated)
            throw new CommentNotFoundException(commentId);

        return commentId;
    }

    // DELETE /v1/comments/{id}
    public async Task DeleteAsync(
        int commentId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissionService.HasRightAsync(userId, "comment_components", cancellationToken))
            throw new CommentPermissionException("Отсутствует право comment_components.");

        var comment = await _commentRepository.GetByIdAsync(commentId, cancellationToken)
            ?? throw new CommentNotFoundException(commentId);

        // Check authorship
        if (comment.AuthorId != userId)
            throw new CommentValidationException("У вас нет прав на удаление этого комментария.");

        // Check not deleted
        if (!comment.IsActive)
            throw new CommentValidationException("Комментарий уже удалён.");

        await _commentRepository.SoftDeleteAsync(commentId, cancellationToken);
    }

    // GET /v1/comments?entityType=X&entityId=Y
    public async Task<IReadOnlyList<CommentListItem>> GetAllByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken cancellationToken = default)
    {
        var comments = await _commentRepository.GetAllByEntityAsync(entityType, entityId, cancellationToken);

        return comments.Select(c => new CommentListItem(
            c.Id,
            c.AuthorId,
            c.Text,
            c.EntityType,
            c.EntityId,
            c.IsActive,
            c.CreatedAt,
            c.UpdatedAt)).ToList();
    }

    private static string ValidateText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new CommentValidationException("Текст комментария не может быть пустым.");

        var normalized = text.Trim();
        if (normalized.Length > 5000)
            throw new CommentValidationException("Текст комментария слишком длинный.");

        return normalized;
    }
}

// DTOs
public sealed record CreateCommentRequest(
    string CommentEntityType,
    int CommentEntityId,
    string CommentText);

public sealed record UpdateCommentRequest(
    string CommentText);

public sealed record CommentListItem(
    int CommentId,
    int AuthorId,
    string Text,
    string EntityType,
    int EntityId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

// Response DTOs
public sealed record CreateCommentResponse(int CommentId);
public sealed record UpdateCommentResponse(int CommentId);

// Exceptions
public sealed class CommentValidationException(string message) : Exception(message);
public sealed class CommentNotFoundException(int id) : Exception($"Комментарий с ID {id} не найден.");
public sealed class CommentPermissionException(string message) : Exception(message);
