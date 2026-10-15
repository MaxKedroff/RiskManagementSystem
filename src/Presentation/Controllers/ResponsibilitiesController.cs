using Application.Org.Responsibilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/org/responsibilities")]
[Authorize(Roles = "ADMIN")]
public sealed class ResponsibilitiesController : ControllerBase
{
    private readonly ResponsibilityService _service;
    private readonly ILogger<ResponsibilitiesController> _logger;

    public ResponsibilitiesController(
        ResponsibilityService service,
        ILogger<ResponsibilitiesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateResponsibilityResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateResponsibilityRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await _service.CreateAsync(
                request.PersonId,
                request.DirectedId,
                request.ResponsibilityType,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new CreateResponsibilityResponse(id));
        }
        catch (ResponsibilityValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create responsibility.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ResponsibilityResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var items = await _service.GetAllAsync(cancellationToken);

            return Ok(items.Select(r => new ResponsibilityResponse(
                r.Id,
                r.PersonId,
                r.DirectedPersonId,
                r.Type,
                r.IsActive)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get responsibilities.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ResponsibilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var r = await _service.GetByIdAsync(id, cancellationToken);

            return Ok(new ResponsibilityResponse(
                r.Id,
                r.PersonId,
                r.DirectedPersonId,
                r.Type,
                r.IsActive));
        }
        catch (ResponsibilityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ResponsibilityValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get responsibility {Id}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UpdateResponsibilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateResponsibilityRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedId = await _service.UpdateAsync(
                id,
                request.PersonId,
                request.DirectedId,
                request.ResponsibilityType,
                request.IsResponsibilityActive,
                cancellationToken);

            return Ok(new UpdateResponsibilityResponse(updatedId));
        }
        catch (ResponsibilityValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ResponsibilityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update responsibility {Id}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeactivateAsync(id, cancellationToken);
            return NoContent();
        }
        catch (ResponsibilityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ResponsibilityValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate responsibility {Id}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}

public sealed record CreateResponsibilityRequest(
    int PersonId,
    int? DirectedId,
    string ResponsibilityType);

public sealed record UpdateResponsibilityRequest(
    int? PersonId,
    int? DirectedId,
    string? ResponsibilityType,
    bool? IsResponsibilityActive);

public sealed record CreateResponsibilityResponse(int ResponsibilityTypeId);
public sealed record UpdateResponsibilityResponse(int ResponsibilityTypeId);

public sealed record ResponsibilityResponse(
    int ResponsibilityTypeId,
    int PersonId,
    int? DirectedId,
    string ResponsibilityType,
    bool IsResponsibilityActive);