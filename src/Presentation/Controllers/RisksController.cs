using Application.Abstractions;
using Application.Risk.Risks;
using Domain.Risk;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/risks")]
[Authorize(Roles = "ADMIN")]
public sealed class RisksController : ControllerBase
{
    private readonly RiskService _service;
    private readonly ILogger<RisksController> _logger;

    public RisksController(
        RiskService service,
        ILogger<RisksController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RiskListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var risks = await _service.GetAllAsync(userId, cancellationToken);

            return Ok(risks);
        }
        catch (RiskPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get risks.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateRiskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRiskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var riskId = await _service.CreateAsync(userId, request, cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new CreateRiskResponse(riskId));
        }
        catch (RiskValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (RiskPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create risk.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UpdateRiskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateRiskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var riskId = await _service.UpdateAsync(id, userId, request, cancellationToken);

            return Ok(new UpdateRiskResponse(riskId));
        }
        catch (RiskValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (RiskNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RiskAuthorizationException ex)
        {
            return Forbid();
        }
        catch (RiskPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update risk {RiskId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/assess")]
    [ProducesResponseType(typeof(AssessRiskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Assess(
        int id,
        [FromBody] AssessRiskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.AssessAsync(id, userId, request, cancellationToken);

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (RiskValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (RiskNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RiskConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (RiskPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assess risk {RiskId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/accept")]
    [ProducesResponseType(typeof(AcceptRiskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(int id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.AcceptAsync(id, userId, cancellationToken);
            return Ok(response);
        }
        catch (RiskValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (RiskNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RiskConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (RiskPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept risk {RiskId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/treat")]
    [ProducesResponseType(typeof(TreatRiskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Treat(int id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.TreatAsync(id, userId, cancellationToken);
            return Ok(response);
        }
        catch (RiskValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (RiskNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RiskPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to treat risk {RiskId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/split")]
    [ProducesResponseType(typeof(SplitRiskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Split(int id, [FromBody] SplitRiskRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.SplitAsync(id, userId, request, cancellationToken);
            return Ok(response);
        }
        catch (RiskValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (RiskNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RiskPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to split risk {RiskId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/transfer")]
    [ProducesResponseType(typeof(TransferRiskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Transfer(int id, [FromBody] TransferRiskRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.TransferAsync(id, userId, request, cancellationToken);
            return Ok(response);
        }
        catch (RiskValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (RiskNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RiskPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transfer risk {RiskId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/reassess")]
    [ProducesResponseType(typeof(ReassessRiskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reassess(
        int id,
        [FromBody] ReassessRiskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.ReassessAsync(id, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (RiskValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (RiskNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RiskPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reassess risk {RiskId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/reset-reassessment")]
    [ProducesResponseType(typeof(ResetReassessmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetReassessment(int id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.ResetForReassessmentAsync(id, userId, cancellationToken);
            return Ok(response);
        }
        catch (RiskValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (RiskNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RiskPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset reassessment for risk {RiskId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/close")]
    [ProducesResponseType(typeof(CloseRiskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Close(int id, [FromBody] CloseRiskRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var response = await _service.CloseAsync(id, userId, request, cancellationToken);
            return Ok(response);
        }
        catch (RiskValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (RiskNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RiskPermissionException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close risk {RiskId}.", id);
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

// Response DTOs
public sealed record CreateRiskResponse(int RiskId);
public sealed record UpdateRiskResponse(int RiskId);
