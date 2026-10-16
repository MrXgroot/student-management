using EduCore.Api.Models;

namespace EduCore.Api.Services;

public interface ITermRegistrationService
{
    bool IsMongoDbActive { get; }

    Task<List<TermRegistration>> GetRegistrationsAsync(string? period, string? program, int? semester, string? status, string? search);

    Task<TermRegistration?> GetRegistrationByIdAsync(string id);

    Task<List<TermRegistration>> GetRegistrationsByStudentIdAsync(string studentId);

    Task<TermRegistrationDetailDto?> GetRegistrationDetailAsync(string id);

    Task<List<StudentTermStatusDto>> GetStudentEligibilityOverviewAsync(string periodCode, string? program, int? semester, string? search);

    Task<TermRegistration> RegisterStudentForTermAsync(CreateTermRegistrationDto dto);

    Task<TermRegistration?> UpdateRegistrationStatusAsync(string id, string newStatus);

    Task<List<CourseEnrollment>> GetEnrolledCoursesAsync(string registrationId);

    Task<List<Course>> GetEligibleCoursesAsync(string registrationId);

    Task<CourseEnrollment> EnrollCourseAsync(string registrationId, string courseIdOrCode);

    Task<bool> DropCourseEnrollmentAsync(string registrationId, string courseId);

    Task<List<CourseEnrollment>> GetEnrollmentsByCourseAsync(string courseId, string? academicPeriodCode = null);
}
