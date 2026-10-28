using Application.Abstractions;
using Application.Risk.Risks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/assessment/likelihood-levels")]
[Authorize]
public sealed class LikelihoodLevelsController : ControllerBase
{
    private readonly RiskService _service;
    private readonly ILogger<LikelihoodLevelsController> _logger;

    public LikelihoodLevelsController(
        RiskService service,
        ILogger<LikelihoodLevelsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LikelihoodLevelItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int methodId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.GetLikelihoodLevelsAsync(methodId, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get likelihood levels.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}
