using Application.Risk.HazardGroups;
using Domain.Risk;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/risk/hazard-groups")]
[Authorize(Roles = "ADMIN")]
public sealed class HazardGroupsController : ControllerBase
{
    private readonly HazardGroupService _service;
    private readonly ILogger<HazardGroupsController> _logger;

    public HazardGroupsController(
        HazardGroupService service,
        ILogger<HazardGroupsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateHazardGroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateHazardGroupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await _service.CreateAsync(
                request.HazardGroupName,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new CreateHazardGroupResponse(id));
        }
        catch (HazardGroupValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HazardGroupConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create hazard group.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HazardGroupResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var hazardGroups = await _service.GetAllAsync(cancellationToken);

            return Ok(hazardGroups.Select(hg => new HazardGroupResponse(
                hg.Id,
                hg.Name,
                hg.IsActive)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get hazard groups.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(HazardGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var hazardGroup = await _service.GetByIdAsync(id, cancellationToken);

            return Ok(new HazardGroupResponse(
                hazardGroup.Id,
                hazardGroup.Name,
                hazardGroup.IsActive));
        }
        catch (HazardGroupNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (HazardGroupValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get hazard group {HazardGroupId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UpdateHazardGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateHazardGroupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedId = await _service.UpdateAsync(
                id,
                request.HazardGroupName,
                request.IsActive,
                cancellationToken);

            return Ok(new UpdateHazardGroupResponse(updatedId));
        }
        catch (HazardGroupValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HazardGroupNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (HazardGroupConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update hazard group {HazardGroupId}.", id);
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
        catch (HazardGroupNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (HazardGroupHasDependenciesException ex)
        {
            return Conflict(new
            {
                message = ex.Message,
                dependencies = new
                {
                    hazards = ex.Dependencies.Hazards
                }
            });
        }
        catch (HazardGroupValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate hazard group {HazardGroupId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}

public sealed record CreateHazardGroupRequest(
    string HazardGroupName);

public sealed record UpdateHazardGroupRequest(
    string? HazardGroupName,
    bool? IsActive);

public sealed record CreateHazardGroupResponse(
    int HazardGroupId);

public sealed record UpdateHazardGroupResponse(
    int HazardGroupId);

public sealed record HazardGroupResponse(
    int HazardGroupId,
    string HazardGroupName,
    bool IsActive);
