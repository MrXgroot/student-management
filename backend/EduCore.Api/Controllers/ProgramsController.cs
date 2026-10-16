using EduCore.Api.Models;
using EduCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduCore.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProgramsController : ControllerBase
{
    private readonly IAcademicService _academicService;

    public ProgramsController(IAcademicService academicService)
    {
        _academicService = academicService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProgramInfo>>> GetPrograms()
    {
        var programs = await _academicService.GetProgramsAsync();
        return Ok(programs);
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<ProgramDetailDto>> GetProgramByCode(string code)
    {
        var programDetails = await _academicService.GetProgramDetailsAsync(code);
        if (programDetails == null)
        {
            return NotFound(new { message = $"Program with code '{code}' not found." });
        }
        return Ok(programDetails);
    }
}
