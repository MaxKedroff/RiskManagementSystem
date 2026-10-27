using Application.Risk.Categories;
using Domain.Risk;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/risk/categories")]
[Authorize]
public sealed class CategoriesController : ControllerBase
{
    private readonly RiskCategoryService _service;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        RiskCategoryService service,
        ILogger<CategoriesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ProducesResponseType(typeof(CreateCategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await _service.CreateAsync(
                request.CategoryName,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new CreateCategoryResponse(id));
        }
        catch (RiskCategoryValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (RiskCategoryConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create category.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _service.GetAllAsync(cancellationToken);

            return Ok(categories.Select(c => new CategoryResponse(
                c.Id,
                c.Name,
                c.IsActive)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get categories.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await _service.GetByIdAsync(id, cancellationToken);

            return Ok(new CategoryResponse(
                category.Id,
                category.Name,
                category.IsActive));
        }
        catch (RiskCategoryNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RiskCategoryValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get category {CategoryId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UpdateCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedId = await _service.UpdateAsync(
                id,
                request.CategoryName,
                request.IsActive,
                cancellationToken);

            return Ok(new UpdateCategoryResponse(updatedId));
        }
        catch (RiskCategoryValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (RiskCategoryNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RiskCategoryConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update category {CategoryId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [Authorize(Roles = "ADMIN")]
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
        catch (RiskCategoryNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (RiskCategoryHasDependenciesException ex)
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
        catch (RiskCategoryValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate category {CategoryId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}

public sealed record CreateCategoryRequest(
    string CategoryName);

public sealed record UpdateCategoryRequest(
    string? CategoryName,
    bool? IsActive);

public sealed record CreateCategoryResponse(
    int CategoryId);

public sealed record UpdateCategoryResponse(
    int CategoryId);

public sealed record CategoryResponse(
    int CategoryId,
    string CategoryName,
    bool IsActive);
