using EduCore.Api.Models;

namespace EduCore.Api.Services;

public interface IAttendanceService
{
    bool IsMongoDbActive { get; }

    Task<CourseAttendanceSheetDto> GetAttendanceSheetAsync(string courseId, DateTime classDate, string? academicPeriodCode = null);

    Task<CourseAttendanceSheetDto> SaveAttendanceAsync(SaveAttendanceBatchDto dto);

    Task<List<AttendanceSessionSummaryDto>> GetAttendanceHistoryAsync(string courseId, string? academicPeriodCode = null);

    Task<List<AttendanceRecord>> GetStudentAttendanceHistoryAsync(string studentId, string? courseId = null, string? academicPeriodCode = null);
}
