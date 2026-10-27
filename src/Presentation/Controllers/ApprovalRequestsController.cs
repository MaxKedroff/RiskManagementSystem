using Application.Treatment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/approval-requests")]
[Authorize]
public sealed class ApprovalRequestsController : ControllerBase
{
    private readonly TreatmentService _service;
    private readonly ILogger<ApprovalRequestsController> _logger;

    public ApprovalRequestsController(
        TreatmentService service,
        ILogger<ApprovalRequestsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(typeof(ApproveResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(
        int id,
        [FromBody] ApproveRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.ApproveAsync(id, userId, request.ApprovalComment, cancellationToken);
            return Ok(response);
        }
        catch (TreatmentValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (TreatmentNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (TreatmentPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve approval request {ApprovalRequestId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(typeof(RejectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(
        int id,
        [FromBody] RejectRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.RejectAsync(id, userId, request.ApprovalReason, cancellationToken);
            return Ok(response);
        }
        catch (TreatmentValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (TreatmentNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (TreatmentPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject approval request {ApprovalRequestId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/revoke")]
    [ProducesResponseType(typeof(RevokeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(
        int id,
        [FromBody] RevokeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.RevokeAsync(id, userId, request.RevokeReason, cancellationToken);
            return Ok(response);
        }
        catch (TreatmentValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (TreatmentNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (TreatmentPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke approval request {ApprovalRequestId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
            throw new UnauthorizedAccessException("User ID not found in token.");

        return userId;
    }
}

// Request DTOs
public sealed record ApproveRequest(string ApprovalComment);
public sealed record RejectRequest(string ApprovalReason);
public sealed record RevokeRequest(string RevokeReason);
