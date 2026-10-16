using EduCore.Api.Models;
using EduCore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduCore.Api.Controllers;

[ApiController]
[Route("api/v1/fees")]
public class FeeController : ControllerBase
{
    private readonly IFeeService _feeService;

    public FeeController(IFeeService feeService)
    {
        _feeService = feeService;
    }

    [HttpGet("structures")]
    public async Task<ActionResult<List<ProgramFeeStructure>>> GetProgramFeeStructures([FromQuery] string? period)
    {
        var structures = await _feeService.GetProgramFeeStructuresAsync(period);
        return Ok(structures);
    }

    [HttpPut("structures/{programCode}")]
    public async Task<ActionResult<ProgramFeeStructure>> UpdateProgramFeeStructure(
        string programCode,
        [FromBody] UpdateProgramFeeDto dto,
        [FromQuery] string? period)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updated = await _feeService.UpdateProgramFeeStructureAsync(programCode, dto, period);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("overview")]
    public async Task<ActionResult<FeeOverviewDto>> GetFeeOverview(
        [FromQuery] string? period,
        [FromQuery] string? program,
        [FromQuery] string? status,
        [FromQuery] string? search)
    {
        try
        {
            var overview = await _feeService.GetFeeOverviewAsync(period, program, status, search);
            return Ok(overview);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("students/{studentId}")]
    public async Task<ActionResult<StudentFeeDetailDto>> GetStudentFeeDetail(
        string studentId,
        [FromQuery] string? period)
    {
        try
        {
            var detail = await _feeService.GetStudentFeeDetailAsync(studentId, period);
            if (detail == null)
            {
                return NotFound(new { message = $"Fee account for student '{studentId}' not found." });
            }
            return Ok(detail);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("payments")]
    public async Task<ActionResult<FeePaymentDto>> RecordPayment([FromBody] RecordPaymentDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var payment = await _feeService.RecordPaymentAsync(dto);
            return CreatedAtAction(nameof(GetStudentFeeDetail), new { studentId = payment.StudentId }, payment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
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

    [HttpGet("payments")]
    public async Task<ActionResult<List<FeePaymentDto>>> GetPaymentHistory(
        [FromQuery] string? studentId,
        [FromQuery] string? period,
        [FromQuery] int limit = 50)
    {
        try
        {
            var history = await _feeService.GetPaymentHistoryAsync(studentId, period, limit);
            return Ok(history);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
