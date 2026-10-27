using Application.Treatment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/treatments")]
[Authorize]
public sealed class TreatmentsController : ControllerBase
{
    private readonly TreatmentService _service;
    private readonly ILogger<TreatmentsController> _logger;

    public TreatmentsController(
        TreatmentService service,
        ILogger<TreatmentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateTreatmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTreatmentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var treatmentId = await _service.CreateAsync(userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new CreateTreatmentResponse(treatmentId, "Planned"));
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
            _logger.LogError(ex, "Failed to create treatment.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UpdateTreatmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTreatmentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var treatmentId = await _service.UpdateAsync(id, userId, request, cancellationToken);
            return Ok(new UpdateTreatmentResponse(treatmentId));
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
            _logger.LogError(ex, "Failed to update treatment {TreatmentId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/submit-approval")]
    [ProducesResponseType(typeof(SubmitApprovalResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitApproval(int id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.SubmitApprovalAsync(id, userId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
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
            _logger.LogError(ex, "Failed to submit treatment {TreatmentId} for approval.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/execute")]
    [ProducesResponseType(typeof(ExecuteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Execute(
        int id,
        [FromBody] ExecuteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.ExecuteAsync(id, userId, request, cancellationToken);
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
            _logger.LogError(ex, "Failed to execute treatment {TreatmentId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/replace-assignee")]
    [ProducesResponseType(typeof(ReplaceAssigneeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceAssignee(
        int id,
        [FromBody] ReplaceAssigneeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.ReplaceAssigneeAsync(id, userId, request, cancellationToken);
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
            _logger.LogError(ex, "Failed to replace assignee for treatment {TreatmentId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/remove-assignee")]
    [ProducesResponseType(typeof(RemoveAssigneeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAssignee(int id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.RemoveAssigneeAsync(id, userId, cancellationToken);
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
            _logger.LogError(ex, "Failed to remove assignee for treatment {TreatmentId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/start")]
    [ProducesResponseType(typeof(StartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Start(int id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.StartAsync(id, userId, cancellationToken);
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
            _logger.LogError(ex, "Failed to start treatment {TreatmentId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/hold")]
    [ProducesResponseType(typeof(HoldResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Hold(int id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.HoldAsync(id, userId, cancellationToken);
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
            _logger.LogError(ex, "Failed to hold treatment {TreatmentId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/resume")]
    [ProducesResponseType(typeof(ResumeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resume(int id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.ResumeAsync(id, userId, cancellationToken);
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
            _logger.LogError(ex, "Failed to resume treatment {TreatmentId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(typeof(CompleteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(
        int id,
        [FromBody] CompleteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.CompleteAsync(id, userId, request, cancellationToken);
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
            _logger.LogError(ex, "Failed to complete treatment {TreatmentId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(CancelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.CancelAsync(id, userId, cancellationToken);
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
            _logger.LogError(ex, "Failed to cancel treatment {TreatmentId}.", id);
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
