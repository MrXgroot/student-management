using EduCore.Api.Models;

namespace EduCore.Api.Services;

public interface IStudentService
{
    Task<List<Student>> GetStudentsAsync(string? search = null, string? status = null, string? program = null);
    Task<Student?> GetStudentByIdAsync(string id);
    Task<Student> CreateStudentAsync(CreateStudentDto dto);
    Task<Student?> UpdateStudentAsync(string id, UpdateStudentDto dto);
    Task<bool> DeleteStudentAsync(string id);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    Task ResetDemoDataAsync();
    bool IsMongoDbActive { get; }
}
