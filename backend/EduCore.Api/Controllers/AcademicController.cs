using EduCore.Api.Models;
using EduCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduCore.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AcademicController : ControllerBase
{
    private readonly IAcademicService _academicService;

    public AcademicController(IAcademicService academicService)
    {
        _academicService = academicService;
    }

    [HttpGet("periods")]
    public async Task<ActionResult<List<AcademicPeriod>>> GetAcademicPeriods()
    {
        var periods = await _academicService.GetAcademicPeriodsAsync();
        return Ok(periods);
    }

    [HttpGet("periods/current")]
    public async Task<ActionResult<AcademicPeriod>> GetCurrentAcademicPeriod()
    {
        var period = await _academicService.GetCurrentAcademicPeriodAsync();
        if (period == null) return NotFound(new { message = "No active academic period found." });
        return Ok(period);
    }
}
