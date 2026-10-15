using Application.Import.FileUploads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("v1/import/uploads")]
[Authorize(Roles = "ADMIN,IMPORT_DATA")]
public sealed class FileUploadsController : ControllerBase
{
    private readonly FileUploadService _service;
    private readonly ILogger<FileUploadsController> _logger;

    public FileUploadsController(
        FileUploadService service,
        ILogger<FileUploadsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateFileUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        [FromBody] UploadFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await _service.UploadAsync(
                request.Title,
                request.PersonId,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new CreateFileUploadResponse(id));
        }
        catch (FileUploadValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file.");
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("{id:int}/confirm")]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirm(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _service.ConfirmAsync(id, cancellationToken);
            return Ok(response);
        }
        catch (FileUploadNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm upload {UploadId}.", id);
            return Problem(statusCode: 500, title: "Внутренняя ошибка сервера.");
        }
    }
}

public sealed record CreateFileUploadResponse(
    int FileUploadId);
