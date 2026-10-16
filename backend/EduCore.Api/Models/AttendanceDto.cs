using System.ComponentModel.DataAnnotations;

namespace EduCore.Api.Models;

public class AttendanceEntryDto
{
    [Required(ErrorMessage = "Student ID is required")]
    public string StudentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Status is required")]
    [RegularExpression(@"^(PRESENT|ABSENT)$", ErrorMessage = "Status must be either PRESENT or ABSENT")]
    public string Status { get; set; } = "PRESENT";

    public string? Remarks { get; set; }
}

public class SaveAttendanceBatchDto
{
    [Required(ErrorMessage = "Academic Period Code is required")]
    public string AcademicPeriodCode { get; set; } = "2026-ODD";

    [Required(ErrorMessage = "Course ID is required")]
    public string CourseId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Class Date is required")]
    public DateTime ClassDate { get; set; } = DateTime.UtcNow.Date;

    [Required(ErrorMessage = "At least one student attendance entry is required")]
    public List<AttendanceEntryDto> Entries { get; set; } = new();
}

public class StudentAttendanceItemDto
{
    public string StudentId { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string? RollNumber { get; set; }
    public string ProgramCode { get; set; } = string.Empty;
    public int CurrentSemester { get; set; } = 1;
    public string? AttendanceRecordId { get; set; }
    public string? Status { get; set; }
    public string? Remarks { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CourseAttendanceSheetDto
{
    public string AcademicPeriodCode { get; set; } = "2026-ODD";
    public string CourseId { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime ClassDate { get; set; } = DateTime.UtcNow.Date;
    public string ClassDateString { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public bool IsRecorded { get; set; }
    public int TotalEnrolled { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public List<StudentAttendanceItemDto> Students { get; set; } = new();
}

public class AttendanceSessionSummaryDto
{
    public string CourseId { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string AcademicPeriodCode { get; set; } = "2026-ODD";
    public DateTime ClassDate { get; set; }
    public string ClassDateString { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}
