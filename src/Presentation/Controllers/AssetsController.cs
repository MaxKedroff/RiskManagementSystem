using Application.Org.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/org/assets")]
[Authorize(Roles = "ADMIN")]
public sealed class AssetsController : ControllerBase
{
    private readonly AssetService _service;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(
        AssetService service,
        ILogger<AssetsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateAssetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAssetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await _service.CreateAsync(
                request.AssetTitle,
                request.AssetInventoryNum,
                request.AssetHourCost,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new CreateAssetResponse(id));
        }
        catch (AssetValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create asset.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AssetResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var assets = await _service.GetAllAsync(cancellationToken);

            return Ok(assets.Select(a => new AssetResponse(
                a.Id,
                a.Title,
                a.InventoryNumber,
                a.HourCost,
                a.IsActive)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get assets.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var asset = await _service.GetByIdAsync(id, cancellationToken);

            return Ok(new AssetResponse(
                asset.Id,
                asset.Title,
                asset.InventoryNumber,
                asset.HourCost,
                asset.IsActive));
        }
        catch (AssetNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AssetValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get asset {AssetId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UpdateAssetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateAssetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedId = await _service.UpdateAsync(
                id,
                request.AssetTitle,
                request.AssetInventoryNum,
                request.AssetHourCost,
                request.IsAssetActive,
                cancellationToken);

            return Ok(new UpdateAssetResponse(updatedId));
        }
        catch (AssetValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (AssetNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AssetHasDependenciesException ex)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update asset {AssetId}.", id);
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
        catch (AssetNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AssetHasDependenciesException ex)
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
        catch (AssetValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate asset {AssetId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}

public sealed record CreateAssetRequest(
    string AssetTitle,
    string? AssetInventoryNum,
    decimal AssetHourCost);

public sealed record UpdateAssetRequest(
    string? AssetTitle,
    string? AssetInventoryNum,
    decimal? AssetHourCost,
    bool? IsAssetActive);

public sealed record CreateAssetResponse(int AssetId);
public sealed record UpdateAssetResponse(int AssetId);

public sealed record AssetResponse(
    int AssetId,
    string AssetTitle,
    string? AssetInventoryNum,
    decimal AssetHourCost,
    bool IsAssetActive);