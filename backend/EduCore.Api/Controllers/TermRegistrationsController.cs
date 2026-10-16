using EduCore.Api.Models;
using EduCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduCore.Api.Controllers;

[ApiController]
[Route("api/v1/registrations")]
public class TermRegistrationsController : ControllerBase
{
    private readonly ITermRegistrationService _registrationService;

    public TermRegistrationsController(ITermRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TermRegistration>>> GetRegistrations(
        [FromQuery] string? period,
        [FromQuery] string? program,
        [FromQuery] int? semester,
        [FromQuery] string? status,
        [FromQuery] string? search)
    {
        var list = await _registrationService.GetRegistrationsAsync(period, program, semester, status, search);
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TermRegistrationDetailDto>> GetRegistrationById(string id)
    {
        var detail = await _registrationService.GetRegistrationDetailAsync(id);
        if (detail == null)
        {
            return NotFound(new { message = $"Term registration '{id}' not found." });
        }
        return Ok(detail);
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<List<TermRegistration>>> GetRegistrationsByStudentId(string studentId)
    {
        var list = await _registrationService.GetRegistrationsByStudentIdAsync(studentId);
        return Ok(list);
    }

    [HttpGet("overview")]
    public async Task<ActionResult<List<StudentTermStatusDto>>> GetStudentEligibilityOverview(
        [FromQuery] string? period,
        [FromQuery] string? program,
        [FromQuery] int? semester,
        [FromQuery] string? search)
    {
        var termCode = string.IsNullOrWhiteSpace(period) ? "2026-ODD" : period;
        var overview = await _registrationService.GetStudentEligibilityOverviewAsync(termCode, program, semester, search);
        return Ok(overview);
    }

    [HttpPost]
    public async Task<ActionResult<TermRegistration>> RegisterStudent([FromBody] CreateTermRegistrationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var created = await _registrationService.RegisterStudentForTermAsync(dto);
            return CreatedAtAction(nameof(GetRegistrationById), new { id = created.Id }, created);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<TermRegistration>> UpdateStatus(string id, [FromBody] UpdateRegistrationStatusDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updated = await _registrationService.UpdateRegistrationStatusAsync(id, dto.Status);
            if (updated == null)
            {
                return NotFound(new { message = $"Term registration '{id}' not found." });
            }
            return Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/courses")]
    public async Task<ActionResult<List<CourseEnrollment>>> GetEnrolledCourses(string id)
    {
        var courses = await _registrationService.GetEnrolledCoursesAsync(id);
        return Ok(courses);
    }

    [HttpGet("{id}/eligible-courses")]
    public async Task<ActionResult<List<Course>>> GetEligibleCourses(string id)
    {
        var courses = await _registrationService.GetEligibleCoursesAsync(id);
        return Ok(courses);
    }

    [HttpPost("{id}/courses")]
    public async Task<ActionResult<CourseEnrollment>> EnrollCourse(string id, [FromBody] EnrollCourseDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var enrollment = await _registrationService.EnrollCourseAsync(id, dto.CourseId);
            return Ok(enrollment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}/courses/{courseId}")]
    public async Task<ActionResult> DropCourse(string id, string courseId)
    {
        try
        {
            var dropped = await _registrationService.DropCourseEnrollmentAsync(id, courseId);
            if (!dropped)
            {
                return NotFound(new { message = "Course enrollment not found or already withdrawn." });
            }
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
