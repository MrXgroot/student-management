using EduCore.Api.Models;
using EduCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduCore.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IStudentService _studentService;

    public DashboardController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var summary = await _studentService.GetDashboardSummaryAsync();
        return Ok(summary);
    }

    [HttpGet("health")]
    public ActionResult GetHealth()
    {
        return Ok(new
        {
            status = "HEALTHY",
            institution = "Vidyadeep Institute of Technology, Bengaluru",
            timestamp = DateTime.UtcNow,
            isUsingMongoDb = _studentService.IsMongoDbActive,
            persistenceMode = _studentService.IsMongoDbActive ? "MongoDB Server (Local)" : "Resilient In-Memory Fallback"
        });
    }
}
