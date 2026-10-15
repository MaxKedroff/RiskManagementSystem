using Application.Org.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/roles")]
[Authorize(Roles = "ADMIN")]
public sealed class RolesController : ControllerBase
{
    private readonly RoleService _service;
    private readonly ILogger<RolesController> _logger;

    public RolesController(
        RoleService service,
        ILogger<RolesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var roles = await _service.GetAllAsync(cancellationToken);

            return Ok(roles.Select(r => new RoleResponse(
                r.Id,
                r.Code,
                r.Name,
                r.IsActive)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get roles.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet("rights")]
    [ProducesResponseType(typeof(IReadOnlyList<RightResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRights(
        [FromQuery] int roleId,
        CancellationToken cancellationToken)
    {
        try
        {
            var rights = await _service.GetRightsAsync(roleId, cancellationToken);

            return Ok(rights.Select(r => new RightResponse(
                r.Id,
                r.Code,
                r.Description)));
        }
        catch (RoleNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RoleValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rights for role {RoleId}.", roleId);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPut("{id:int}/rights")]
    [ProducesResponseType(typeof(UpdateRoleRightsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRights(
        int id,
        [FromBody] UpdateRoleRightsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedId = await _service.UpdateRightsAsync(
                id,
                request.RightIds,
                cancellationToken);

            return Ok(new UpdateRoleRightsResponse(updatedId));
        }
        catch (RoleValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (RoleNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RightNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update rights for role {RoleId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}

[ApiController]
[Route("v1/rights")]
[Authorize(Roles = "ADMIN")]
public sealed class RightsController : ControllerBase
{
    private readonly RoleService _service;
    private readonly ILogger<RightsController> _logger;

    public RightsController(
        RoleService service,
        ILogger<RightsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RightResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var rights = await _service.GetAllRightsAsync(cancellationToken);

            return Ok(rights.Select(r => new RightResponse(
                r.Id,
                r.Code,
                r.Description)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rights.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}

public sealed record RoleResponse(
    int RoleId,
    string RoleCode,
    string RoleName,
    bool IsRoleActive);

public sealed record RightResponse(
    int RightId,
    string RightCode,
    string RightDescription);

public sealed record UpdateRoleRightsRequest(
    IReadOnlyList<int> RightIds);

public sealed record UpdateRoleRightsResponse(int RoleId);