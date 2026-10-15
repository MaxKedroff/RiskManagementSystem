using Application.Org.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/users")]
[Authorize(Roles = "ADMIN")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _service;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserService service,
        ILogger<UsersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await _service.CreateAsync(
                request.UserFio,
                request.UserEmail,
                request.RoleIds,
                request.PersonId,
                request.DirectorateId,
                request.ResponsibilityType,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new CreateUserResponse(id));
        }
        catch (UserValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var users = await _service.GetAllAsync(cancellationToken);

            var result = new List<UserResponse>();

            foreach (var user in users)
            {
                var (u, roles) = await _service.GetByIdAsync(user.Id, cancellationToken);
                result.Add(new UserResponse(
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.IsActive,
                    roles.Select(r => r.Code).ToArray()));
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var (user, roles) = await _service.GetByIdAsync(id, cancellationToken);

            return Ok(new UserDetailsResponse(
                user.Id,
                user.FullName,
                user.Email,
                user.IsActive,
                roles.Select(r => r.Code).ToArray(),
                user.PersonId,
                user.DirectorateId));
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UserValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user {UserId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UpdateUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedId = await _service.UpdateAsync(
                id,
                request.UserFio,
                request.RoleIds,
                request.IsUserActive,
                cancellationToken);

            return Ok(new UpdateUserResponse(updatedId));
        }
        catch (UserValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UserLastAdminException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/reset-password")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var temporaryPassword = await _service.ResetPasswordAsync(id, cancellationToken);

            return Ok(new ResetPasswordResponse(temporaryPassword));
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UserValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset password for user {UserId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}

public sealed record CreateUserRequest(
    string UserFio,
    string UserEmail,
    IReadOnlyList<int> RoleIds,
    int? PersonId,
    int? DirectorateId,
    string? ResponsibilityType);

public sealed record UpdateUserRequest(
    string? UserFio,
    IReadOnlyList<int>? RoleIds,
    bool? IsUserActive);

public sealed record CreateUserResponse(int UserId);
public sealed record UpdateUserResponse(int UserId);
public sealed record ResetPasswordResponse(string TemporaryPassword);

public sealed record UserResponse(
    int UserId,
    string FullName,
    string Email,
    bool IsUserActive,
    IReadOnlyList<string> Roles);

public sealed record UserDetailsResponse(
    int UserId,
    string FullName,
    string Email,
    bool IsUserActive,
    IReadOnlyList<string> Roles,
    int? PersonId,
    int? DirectorateId);