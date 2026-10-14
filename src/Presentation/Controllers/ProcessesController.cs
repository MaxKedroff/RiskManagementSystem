using Application.Org.Processes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/org/processes")]
[Authorize(Roles = "ADMIN")]
public sealed class ProcessesController : ControllerBase
{
    private readonly ProcessService _service;
    private readonly ILogger<ProcessesController> _logger;

    public ProcessesController(
        ProcessService service,
        ILogger<ProcessesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateProcessResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProcessRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await _service.CreateAsync(
                request.DirectorateId,
                request.ProcessTitle,
                request.OwnerId,
                request.ProcessDescription,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new CreateProcessResponse(id));
        }
        catch (ProcessValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create process.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProcessResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var processes = await _service.GetAllAsync(cancellationToken);

            return Ok(processes.Select(p => new ProcessResponse(
                p.Id,
                p.DirectorateId,
                p.Title,
                p.OwnerId,
                p.Description,
                p.IsActive)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get processes.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProcessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var process = await _service.GetByIdAsync(id, cancellationToken);

            return Ok(new ProcessResponse(
                process.Id,
                process.DirectorateId,
                process.Title,
                process.OwnerId,
                process.Description,
                process.IsActive));
        }
        catch (ProcessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ProcessValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get process {ProcessId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UpdateProcessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProcessRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedId = await _service.UpdateAsync(
                id,
                request.DirectorateId,
                request.ProcessTitle,
                request.OwnerId,
                request.ProcessDescription,
                request.IsProcessActive,
                cancellationToken);

            return Ok(new UpdateProcessResponse(updatedId));
        }
        catch (ProcessValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ProcessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ProcessHasDependenciesException ex)
        {
            return Conflict(new
            {
                message = ex.Message,
                dependencies = new
                {
                    riskProcesses = ex.Dependencies.RiskProcesses,
                    responsibilities = ex.Dependencies.Responsibilities,
                    ownedProcesses = ex.Dependencies.OwnedProcesses
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update process {ProcessId}.", id);
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
        catch (ProcessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ProcessHasDependenciesException ex)
        {
            return Conflict(new
            {
                message = ex.Message,
                dependencies = new
                {
                    riskProcesses = ex.Dependencies.RiskProcesses,
                    responsibilities = ex.Dependencies.Responsibilities,
                    ownedProcesses = ex.Dependencies.OwnedProcesses
                }
            });
        }
        catch (ProcessValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate process {ProcessId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}

public sealed record CreateProcessRequest(
    int DirectorateId,
    string ProcessTitle,
    int OwnerId,
    string? ProcessDescription);

public sealed record UpdateProcessRequest(
    int? DirectorateId,
    string? ProcessTitle,
    int? OwnerId,
    string? ProcessDescription,
    bool? IsProcessActive);

public sealed record CreateProcessResponse(int ProcessId);
public sealed record UpdateProcessResponse(int ProcessId);

public sealed record ProcessResponse(
    int ProcessId,
    int DirectorateId,
    string ProcessTitle,
    int OwnerId,
    string? ProcessDescription,
    bool IsProcessActive);