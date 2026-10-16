using System.ComponentModel.DataAnnotations;

namespace EduCore.Api.Models;

public class CreateTermRegistrationDto
{
    [Required(ErrorMessage = "Student ID is required")]
    public string StudentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Academic Period Code is required")]
    public string AcademicPeriodCode { get; set; } = "2026-ODD";

    [Required(ErrorMessage = "Semester is required")]
    [Range(1, 8, ErrorMessage = "Semester must be between 1 and 8")]
    public int Semester { get; set; } = 1;
}

public class UpdateRegistrationStatusDto
{
    [Required(ErrorMessage = "Status is required")]
    [RegularExpression(@"^(REGISTERED|CONFIRMED|WITHDRAWN)$", ErrorMessage = "Status must be REGISTERED, CONFIRMED, or WITHDRAWN")]
    public string Status { get; set; } = "CONFIRMED";
}

public class EnrollCourseDto
{
    [Required(ErrorMessage = "Course ID or Code is required")]
    public string CourseId { get; set; } = string.Empty;
}

public class TermRegistrationDetailDto
{
    public TermRegistration Registration { get; set; } = new();
    public Student? Student { get; set; }
    public List<CourseEnrollment> EnrolledCourses { get; set; } = new();
    public int TotalCredits { get; set; }

    public string Id => Registration?.Id ?? string.Empty;
    public string StudentId => Registration?.StudentId ?? string.Empty;
    public string StudentName => Registration?.StudentName ?? Student?.FullName ?? string.Empty;
    public string AdmissionNumber => Registration?.AdmissionNumber ?? Student?.AdmissionNumber ?? string.Empty;
    public string ProgramCode => Registration?.ProgramCode ?? Student?.Program ?? string.Empty;
    public int Semester => Registration?.Semester ?? 1;
    public string AcademicPeriodCode => Registration?.AcademicPeriodCode ?? string.Empty;
    public string Status => Registration?.Status ?? "REGISTERED";
}

public class StudentTermStatusDto
{
    public Student Student { get; set; } = new();
    public bool IsRegistered { get; set; }
    public TermRegistration? Registration { get; set; }
    public int EnrolledCourseCount { get; set; }
    public int TotalCredits { get; set; }

    public string StudentId => Student?.Id ?? string.Empty;
    public string StudentName => Student?.FullName ?? string.Empty;
    public string AdmissionNumber => Student?.AdmissionNumber ?? string.Empty;
    public string? RollNumber => Student?.RollNumber;
    public string ProgramCode => Student?.Program ?? string.Empty;
    public string StudentLifecycleStatus => Student?.LifecycleStatus ?? string.Empty;
    public string? TermRegistrationId => Registration?.Id;
    public string? RegistrationStatus => Registration?.Status;
    public int? RegisteredSemester => Registration?.Semester;
    public string? AcademicPeriodCode => Registration?.AcademicPeriodCode;
    public int TotalEnrolledCredits => TotalCredits;
}
