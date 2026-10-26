using Application.Abstractions;
using Application.Comments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/comments")]
[Authorize(Roles = "ADMIN")]
public sealed class CommentsController : ControllerBase
{
    private readonly CommentService _service;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(
        CommentService service,
        ILogger<CommentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateCommentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var commentId = await _service.CreateAsync(userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new CreateCommentResponse(commentId));
        }
        catch (CommentValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (CommentPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create comment.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UpdateCommentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateCommentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var commentId = await _service.UpdateAsync(id, userId, request, cancellationToken);
            return Ok(new UpdateCommentResponse(commentId));
        }
        catch (CommentValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (CommentNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (CommentPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update comment {CommentId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            await _service.DeleteAsync(id, userId, cancellationToken);
            return NoContent();
        }
        catch (CommentValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (CommentNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (CommentPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete comment {CommentId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CommentListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllByEntity(
        [FromQuery] string entityType,
        [FromQuery] int entityId,
        CancellationToken cancellationToken)
    {
        try
        {
            var comments = await _service.GetAllByEntityAsync(entityType, entityId, cancellationToken);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get comments for {EntityType}/{EntityId}.", entityType, entityId);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token.");
        }
        return userId;
    }
}
