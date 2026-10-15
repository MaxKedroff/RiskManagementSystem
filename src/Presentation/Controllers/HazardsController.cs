using Application.Risk.Hazards;
using Domain.Risk;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/risk/hazards")]
[Authorize(Roles = "ADMIN")]
public sealed class HazardsController : ControllerBase
{
    private readonly HazardService _service;
    private readonly ILogger<HazardsController> _logger;

    public HazardsController(
        HazardService service,
        ILogger<HazardsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateHazardResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateHazardRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await _service.CreateAsync(
                request.HazardGroupId,
                request.HazardName,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new CreateHazardResponse(id));
        }
        catch (HazardValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HazardConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create hazard.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HazardResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var hazards = await _service.GetAllAsync(cancellationToken);

            return Ok(hazards.Select(h => new HazardResponse(
                h.Id,
                h.HazardGroupId,
                h.Name,
                h.IsActive)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get hazards.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(HazardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var hazard = await _service.GetByIdAsync(id, cancellationToken);

            return Ok(new HazardResponse(
                hazard.Id,
                hazard.HazardGroupId,
                hazard.Name,
                hazard.IsActive));
        }
        catch (HazardNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (HazardValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get hazard {HazardId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UpdateHazardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateHazardRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedId = await _service.UpdateAsync(
                id,
                request.HazardGroupId,
                request.HazardName,
                request.IsActive,
                cancellationToken);

            return Ok(new UpdateHazardResponse(updatedId));
        }
        catch (HazardValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HazardNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (HazardConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update hazard {HazardId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeactivateAsync(id, cancellationToken);
            return NoContent();
        }
        catch (HazardNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (HazardHasDependenciesException ex)
        {
            return Conflict(new
            {
                message = ex.Message,
                dependencies = new
                {
                    risks = ex.Dependencies.Risks
                }
            });
        }
        catch (HazardValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate hazard {HazardId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}

public sealed record CreateHazardRequest(
    int HazardGroupId,
    string HazardName);

public sealed record UpdateHazardRequest(
    int? HazardGroupId,
    string? HazardName,
    bool? IsActive);

public sealed record CreateHazardResponse(
    int HazardId);

public sealed record UpdateHazardResponse(
    int HazardId);

public sealed record HazardResponse(
    int HazardId,
    int HazardGroupId,
    string HazardName,
    bool IsActive);
