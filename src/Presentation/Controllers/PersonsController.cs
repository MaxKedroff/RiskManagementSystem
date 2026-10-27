using Application.Org.Persons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/org/persons")]
[Authorize]
public sealed class PersonsController : ControllerBase
{
    private readonly PersonService _service;
    private readonly ILogger<PersonsController> _logger;

    public PersonsController(
        PersonService service,
        ILogger<PersonsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ProducesResponseType(typeof(CreatePersonResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePersonRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await _service.CreateAsync(
                request.UserId,
                request.DirectorateId,
                request.PersonPosition,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new CreatePersonResponse(id));
        }
        catch (PersonValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create person.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PersonResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var persons = await _service.GetAllAsync(cancellationToken);

            return Ok(persons.Select(p => new PersonResponse(
                p.Id,
                p.UserId,
                p.DirectorateId,
                p.Position,
                p.IsActive)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get persons.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
    int id,
    [FromBody] UpdatePersonRequest request,
    CancellationToken cancellationToken)
    {
        try
        {
            var updatedId = await _service.UpdateAsync(
                id,
                request.DirectorateId,
                request.PersonPosition,
                request.IsPersonActive,
                cancellationToken);

            return Ok(new UpdatePersonResponse(updatedId));
        }
        catch (PersonValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (PersonNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update person {PersonId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [Authorize(Roles = "ADMIN")]
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
        catch (PersonNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (PersonHasDependenciesException ex)
        {
            return Conflict(new
            {
                message = ex.Message,
                dependencies = new
                {
                    processes = ex.Dependencies.OwnedProcesses,
                    responsibilities = ex.Dependencies.Responsibilities,
                    riskProcesses = ex.Dependencies.RiskProcesses
                }
            });
        }
        catch (PersonValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate person {PersonId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}

public sealed record CreatePersonRequest(
    int UserId,
    int DirectorateId,
    string PersonPosition);

public sealed record UpdatePersonRequest(
    int? DirectorateId,
    string? PersonPosition,
    bool? IsPersonActive);

public sealed record CreatePersonResponse(int PersonId);
public sealed record UpdatePersonResponse(int PersonId);

public sealed record PersonResponse(
    int PersonId,
    int? UserId,
    int? DirectorateId,
    string PersonPosition,
    bool IsPersonActive);
