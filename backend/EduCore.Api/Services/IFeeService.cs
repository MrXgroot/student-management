using EduCore.Api.Models;

namespace EduCore.Api.Services;

public interface IFeeService
{
    bool IsMongoDbActive { get; }

    Task<List<ProgramFeeStructure>> GetProgramFeeStructuresAsync(string? academicPeriodCode = null);

    Task<ProgramFeeStructure> UpdateProgramFeeStructureAsync(string programCode, UpdateProgramFeeDto dto, string? academicPeriodCode = null);

    Task<FeeOverviewDto> GetFeeOverviewAsync(string? period, string? program, string? status, string? search);

    Task<StudentFeeDetailDto?> GetStudentFeeDetailAsync(string studentId, string? academicPeriodCode = null);

    Task<FeePaymentDto> RecordPaymentAsync(RecordPaymentDto dto);

    Task<List<FeePaymentDto>> GetPaymentHistoryAsync(string? studentId = null, string? period = null, int limit = 50);
}
