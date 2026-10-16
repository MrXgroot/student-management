using EduCore.Api.Models;
using EduCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduCore.Api.Controllers;

[ApiController]
[Route("api/v1/attendance")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpGet]
    public async Task<ActionResult<CourseAttendanceSheetDto>> GetAttendanceSheet(
        [FromQuery] string courseId,
        [FromQuery] DateTime? date,
        [FromQuery] string? period)
    {
        if (string.IsNullOrWhiteSpace(courseId))
        {
            return BadRequest(new { message = "Course ID or code is required." });
        }

        try
        {
            var targetDate = date ?? DateTime.UtcNow.Date;
            var sheet = await _attendanceService.GetAttendanceSheetAsync(courseId, targetDate, period);
            return Ok(sheet);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<CourseAttendanceSheetDto>> SaveAttendance([FromBody] SaveAttendanceBatchDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var sheet = await _attendanceService.SaveAttendanceAsync(dto);
            return Ok(sheet);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<AttendanceSessionSummaryDto>>> GetAttendanceHistory(
        [FromQuery] string courseId,
        [FromQuery] string? period)
    {
        if (string.IsNullOrWhiteSpace(courseId))
        {
            return BadRequest(new { message = "Course ID or code is required." });
        }

        try
        {
            var history = await _attendanceService.GetAttendanceHistoryAsync(courseId, period);
            return Ok(history);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<List<AttendanceRecord>>> GetStudentAttendance(
        string studentId,
        [FromQuery] string? courseId,
        [FromQuery] string? period)
    {
        try
        {
            var records = await _attendanceService.GetStudentAttendanceHistoryAsync(studentId, courseId, period);
            return Ok(records);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
