using EduCore.Api.Models;
using EduCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduCore.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly IAcademicService _academicService;

    public CoursesController(IAcademicService academicService)
    {
        _academicService = academicService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Course>>> GetCourses(
        [FromQuery] string? search,
        [FromQuery] string? program,
        [FromQuery] int? semester,
        [FromQuery] string? status = "ALL")
    {
        var courses = await _academicService.GetCoursesAsync(search, program, semester, status);
        return Ok(courses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Course>> GetCourseById(string id)
    {
        var course = await _academicService.GetCourseByIdAsync(id);
        if (course == null)
        {
            return NotFound(new { message = $"Course with ID '{id}' not found." });
        }
        return Ok(course);
    }

    [HttpPost]
    public async Task<ActionResult<Course>> CreateCourse([FromBody] CreateCourseDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var created = await _academicService.CreateCourseAsync(dto);
            return CreatedAtAction(nameof(GetCourseById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Course>> UpdateCourse(string id, [FromBody] UpdateCourseDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updated = await _academicService.UpdateCourseAsync(id, dto);
            if (updated == null)
            {
                return NotFound(new { message = $"Course with ID '{id}' not found." });
            }
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCourse(string id)
    {
        var deleted = await _academicService.DeleteCourseAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = $"Course with ID '{id}' not found." });
        }
        return NoContent();
    }

    [HttpPost("seed-reset")]
    public async Task<ActionResult> ResetCoursesDemoData()
    {
        await _academicService.ResetAcademicDemoDataAsync();
        return Ok(new { message = "Academic demo data reset successfully to default seeds." });
    }
}
