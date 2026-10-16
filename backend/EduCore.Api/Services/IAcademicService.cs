using EduCore.Api.Models;

namespace EduCore.Api.Services;

public interface IAcademicService
{
    bool IsMongoDbActive { get; }
    Task<List<ProgramInfo>> GetProgramsAsync();
    Task<ProgramDetailDto?> GetProgramDetailsAsync(string code);
    Task<List<AcademicPeriod>> GetAcademicPeriodsAsync();
    Task<AcademicPeriod?> GetCurrentAcademicPeriodAsync();
    Task<List<Course>> GetCoursesAsync(string? search = null, string? program = null, int? semester = null, string? status = null);
    Task<Course?> GetCourseByIdAsync(string id);
    Task<Course?> GetCourseByCodeAsync(string code);
    Task<Course> CreateCourseAsync(CreateCourseDto dto);
    Task<Course?> UpdateCourseAsync(string id, UpdateCourseDto dto);
    Task<bool> DeleteCourseAsync(string id);
    Task ResetAcademicDemoDataAsync();
}
