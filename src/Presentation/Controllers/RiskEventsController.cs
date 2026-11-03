using Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/risk-events")]
[Authorize]
public sealed class RiskEventsController : ControllerBase
{
    private readonly IRiskEventRepository _repository;
    private readonly ILogger<RiskEventsController> _logger;

    public RiskEventsController(
        IRiskEventRepository repository,
        ILogger<RiskEventsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>Risk history, newest first: of one risk, or of all risks when riskId is omitted.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RiskEventItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? riskId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _repository.GetAllAsync(riskId, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get risk events.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}
