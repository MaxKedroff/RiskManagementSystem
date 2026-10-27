using Application.Org.Directorates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/org/directorates")]
[Authorize(Roles = "ADMIN")]
public sealed class DirectoratesController : ControllerBase
{
    private readonly DirectorateService _service;

    public DirectoratesController(DirectorateService service)
    {
        _service = service;
    }

    public sealed record CreateDirectorateRequest(string? DirectorateTitle);

    public sealed record UpdateDirectorateRequest(
        string? DirectorateTitle,
        bool? IsDirectorateActive);

    public sealed record DirectorateResponse(
        int DirectorateId,
        string DirectorateTitle,
        bool IsDirectorateActive);

    public sealed record CreatedDirectorateResponse(int DirectorateId);

    public sealed record UpdatedDirectorateResponse(int DirectorateId);

    public sealed record ApiError(
        string Code,
        string Message,
        object? Details = null);

    [HttpPost]
    [ProducesResponseType(typeof(CreatedDirectorateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDirectorateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await _service.CreateAsync(
                request.DirectorateTitle, cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new CreatedDirectorateResponse(id));
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            return MapException(ex);
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DirectorateResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var directorates = await _service.GetAllAsync(cancellationToken);

        return Ok(directorates.Select(d => new DirectorateResponse(
            d.Id,
            d.Title,
            d.IsActive)));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UpdatedDirectorateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateDirectorateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedId = await _service.UpdateAsync(
                id,
                request.DirectorateTitle,
                request.IsDirectorateActive,
                cancellationToken);

            return Ok(new UpdatedDirectorateResponse(updatedId));
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            return MapException(ex);
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeactivateAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            return MapException(ex);
        }
    }

    private static bool IsExpected(Exception ex) =>
        ex is DirectorateValidationException
            or DirectorateNotFoundException
            or DirectorateConflictException
            or DirectorateHasDependenciesException;

    private IActionResult MapException(Exception ex) =>
        ex switch
        {
            DirectorateValidationException =>
                BadRequest(new ApiError("validation_error", ex.Message)),

            DirectorateNotFoundException =>
                NotFound(new ApiError("not_found", ex.Message)),

            DirectorateHasDependenciesException dependencyException =>
                Conflict(new ApiError(
                    "directorate_has_dependencies",
                    dependencyException.Message,
                    new
                    {
                        persons = dependencyException.Dependencies.Persons,
                        processes = dependencyException.Dependencies.Processes,
                        responsibilities = dependencyException.Dependencies.Responsibilities
                    })),

            DirectorateConflictException =>
                Conflict(new ApiError("conflict", ex.Message)),

            _ => StatusCode(
                StatusCodes.Status500InternalServerError,
                new ApiError("internal_error", "Внутренняя ошибка сервера."))
        };
}
