using Application.Treatment.ApprovalSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/treatment/approval-settings")]
[Authorize]
public sealed class ApprovalSettingsController : ControllerBase
{
    private readonly TreatmentApprovalSettingsService _service;
    private readonly ILogger<ApprovalSettingsController> _logger;

    public ApprovalSettingsController(
        TreatmentApprovalSettingsService service,
        ILogger<ApprovalSettingsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TreatmentApprovalSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _service.GetAsync(cancellationToken);
            return Ok(settings);
        }
        catch (ApprovalSettingsNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get approval settings.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ProducesResponseType(typeof(SaveApprovalSettingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Save(
        [FromBody] SaveApprovalSettingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var settingId = await _service.SaveAsync(
                request.OwnerThreshold,
                request.DirectorThreshold,
                request.CeoThreshold,
                cancellationToken);

            return Ok(new SaveApprovalSettingsResponse(settingId));
        }
        catch (ApprovalSettingsValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save approval settings.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}

public sealed record SaveApprovalSettingsResponse(int TreatmentApprovalSetId);
