using EduCore.Api.Models;
using EduCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduCore.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(IStudentService studentService, ILogger<StudentsController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Student>>> GetAll([FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? program)
    {
        var students = await _studentService.GetStudentsAsync(search, status, program);
        return Ok(students);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Student>> GetById(string id)
    {
        var student = await _studentService.GetStudentByIdAsync(id);
        if (student == null)
        {
            return NotFound(new { message = $"Student with ID '{id}' was not found." });
        }
        return Ok(student);
    }

    [HttpPost]
    public async Task<ActionResult<Student>> Create([FromBody] CreateStudentDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var student = await _studentService.CreateStudentAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = student.Id }, student);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Student>> Update(string id, [FromBody] UpdateStudentDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updated = await _studentService.UpdateStudentAsync(id, dto);
        if (updated == null)
        {
            return NotFound(new { message = $"Student with ID '{id}' was not found." });
        }

        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var deleted = await _studentService.DeleteStudentAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = $"Student with ID '{id}' was not found." });
        }
        return NoContent();
    }

    [HttpPost("seed-reset")]
    public async Task<ActionResult> ResetDemo()
    {
        await _studentService.ResetDemoDataAsync();
        return Ok(new { message = "Demo student records successfully reset." });
    }
}
