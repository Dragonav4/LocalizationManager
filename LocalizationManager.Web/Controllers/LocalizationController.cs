using System.Text;
using System.Text.Json;
using LocalizationManager.Web.DataLayer.ViewModels;
using LocalizationManager.Web.Exceptions;
using LocalizationManager.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalizationManager.Web.Controllers;

[ApiController]
[Route("api/localization")]
public class LocalizationController : ControllerBase
{
    private readonly ILocalizationService _service;

    public LocalizationController(ILocalizationService service)
    {
        _service = service;
    }

    [HttpGet("groups")]
    public async Task<ActionResult<IReadOnlyCollection<string>>> GetGroups(
        [FromQuery] string env,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(env)) throw new BadRequestException("env is required.");

        var groups = await _service.GetGroupsAsync(env, search, cancellationToken);
        return Ok(groups);
    }

    [HttpGet("cultures")]
    public async Task<ActionResult<IReadOnlyCollection<CultureVm>>> GetCultures(
        CancellationToken cancellationToken)
    {
        var list = await _service.GetCulturesAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("records")]
    public async Task<ActionResult<ILocalizationService.PagedResult<LocalizationRecordVm>>> GetRecords(
        [FromQuery] string env,
        [FromQuery] string culture,
        [FromQuery] string? group,
        [FromQuery] string? search,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if(string.IsNullOrWhiteSpace(env)) throw new BadRequestException("env is required.");
        if(string.IsNullOrWhiteSpace(culture)) throw new BadRequestException("culture is required.");
        var res = await _service.GetRecordsAsync(env,culture, group, search,page, pageSize, cancellationToken);
        return Ok(res);
    }

    [HttpGet("records/{id:guid}")]
    public async Task<ActionResult<LocalizationRecordVm>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var vm = await _service.GetByIdAsync(id, cancellationToken);
            return Ok(vm);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("records")]
    public async Task<ActionResult<LocalizationRecordVm>> Upsert(
        [FromQuery] string env,
        [FromQuery] string? group,
        [FromBody] LocalizationRecordUpsertVm vm,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(env)) throw new BadRequestException("env is required.");
        if(vm is null) throw new BadRequestException("vm is required.");

        var saved = await _service.UpsertAsync(env, vm, group, cancellationToken);
        return Ok(saved);
    }

    [HttpDelete("records/{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _service.DeleteAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    //edit form
    [HttpGet("culture-view")]
    public async Task<ActionResult<CultureLocalizationView>> GetCultureView(
        [FromQuery] string env,
        [FromQuery] string culture,
        [FromQuery] string? group,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        if(string.IsNullOrWhiteSpace(env)) throw new BadRequestException("env is required.");
        var vm = await _service.GetCultureViewAsync(env, culture, group, search, cancellationToken);
        return Ok(vm);
    }

    [HttpPost("copy")]
    public async Task<ActionResult<int>> CopyEnv(
        [FromQuery] string fromEnv,
        [FromQuery] string toEnv,
        [FromQuery] string? group,
        [FromQuery] bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(fromEnv)) throw new BadRequestException("fromEnv is required.");
        if(string.IsNullOrWhiteSpace(toEnv)) throw new BadRequestException("toEnv is required.");
        var affected = await _service.CopyEnvAsync(fromEnv, toEnv, group, overwrite, cancellationToken);
        return Ok(affected);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string env,
        [FromQuery] string cultures,
        [FromQuery] string? group,
        CancellationToken cancellationToken)
    {
        if(string.IsNullOrWhiteSpace(env)) throw new BadRequestException("env is required.");
        if(string.IsNullOrWhiteSpace(cultures)) throw new BadRequestException("cultures is required.");
        
        var list = cultures.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        var json = await _service.ExportJsonAsync(env, list, group,cancellationToken);
        return File(Encoding.UTF8.GetBytes(json), "application/json; charset=utf-8", $"loc-{env}.json");
    }

    [HttpPost("import")]
    public async Task<ActionResult<int>> Import([FromQuery] string env,
        [FromBody] JsonElement body,
        CancellationToken cancellationToken,
        [FromQuery] bool overwrite = false,
        [FromQuery] string? group = null)
    {
        if(string.IsNullOrWhiteSpace(env)) throw new BadRequestException("env is required.");
        var json = body.GetRawText(); //todo check method
        var affected = await _service.ImportJsonAsync(env,json,overwrite,group,cancellationToken);
        return Ok(affected);
    }

    [HttpGet("pivot")]
    public async Task<ActionResult<Dictionary<string, Dictionary<string, string>>>> GetPivot(
        [FromQuery] string env,
        [FromQuery] string cultures,
        [FromQuery] string? group,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var list = cultures.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var vm = await _service.GetPivotAsync(env, list, group, search, cancellationToken);
        return Ok(vm);
    }
    
    public sealed record HtmlPreviewRequest(string Html);

    [HttpPost("preview/html")]
    public async Task<IActionResult> PreviewHtml(
        [FromBody] HtmlPreviewRequest request, CancellationToken cancellationToken)
    {
        var safe = await _service.SanitizeHtmlAsync(request.Html,cancellationToken);
        return Content(safe, "text/html; charset=utf-8");
    }
}