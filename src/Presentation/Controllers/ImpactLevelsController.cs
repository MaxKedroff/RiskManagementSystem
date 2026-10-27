using Application.Assessment.ImpactLevels;
using Domain.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/assessment/impact-levels")]
[Authorize]
public sealed class ImpactLevelsController : ControllerBase
{
    private readonly ImpactLevelService _service;
    private readonly ILogger<ImpactLevelsController> _logger;

    public ImpactLevelsController(
        ImpactLevelService service,
        ILogger<ImpactLevelsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ImpactLevelResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int methodId,
        CancellationToken cancellationToken)
    {
        try
        {
            var levels = await _service.GetAllAsync(methodId, cancellationToken);
            return Ok(levels);
        }
        catch (ImpactLevelValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get impact levels.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Save(
        [FromBody] SaveImpactLevelsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _service.SaveAsync(
                request.MethodId,
                request.Levels,
                cancellationToken);

            return Ok(new { message = "Impact levels saved." });
        }
        catch (ImpactLevelValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save impact levels.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}

public sealed record SaveImpactLevelsRequest(
    int MethodId,
    IReadOnlyList<ImpactLevelUpdateRequest> Levels);
