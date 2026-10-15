using Application.Assessment.AcceptanceSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/assessment/acceptance-settings")]
[Authorize(Roles = "ADMIN")]
public sealed class AcceptanceSettingsController : ControllerBase
{
    private readonly RiskAcceptanceSettingsService _service;
    private readonly ILogger<AcceptanceSettingsController> _logger;

    public AcceptanceSettingsController(
        RiskAcceptanceSettingsService service,
        ILogger<AcceptanceSettingsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(RiskAcceptanceSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        [FromQuery] int methodId,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _service.GetAsync(methodId, cancellationToken);
            return Ok(settings);
        }
        catch (AcceptanceSettingsNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AcceptanceSettingsValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get acceptance settings.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(SaveAcceptanceSettingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Save(
        [FromBody] SaveAcceptanceSettingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var settingId = await _service.SaveAsync(
                request.MethodId,
                request.OwnerMaxScore,
                request.DirectorMaxScore,
                request.CeoMaxScore,
                cancellationToken);

            return Ok(new SaveAcceptanceSettingsResponse(settingId));
        }
        catch (AcceptanceSettingsValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save acceptance settings.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}

public sealed record SaveAcceptanceSettingsResponse(int SettingId);
