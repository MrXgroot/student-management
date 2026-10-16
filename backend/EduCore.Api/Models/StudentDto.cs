using System.ComponentModel.DataAnnotations;

namespace EduCore.Api.Models;

public class CreateStudentDto
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Phone number must be a valid 10-digit Indian mobile number")]
    public string Phone { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public string Gender { get; set; } = "Male";

    [Required(ErrorMessage = "Program is required")]
    public string Program { get; set; } = "BCA";

    [Range(1, 8, ErrorMessage = "Semester must be between 1 and 8")]
    public int CurrentSemester { get; set; } = 1;

    public string Address { get; set; } = string.Empty;

    public string GuardianName { get; set; } = string.Empty;

    public string GuardianPhone { get; set; } = string.Empty;
}

public class UpdateStudentDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public string Gender { get; set; } = "Male";

    [Required]
    public string Program { get; set; } = "BCA";

    [Range(1, 8)]
    public int CurrentSemester { get; set; } = 1;

    public string LifecycleStatus { get; set; } = "ACTIVE";

    public string? RollNumber { get; set; }

    public string Address { get; set; } = string.Empty;

    public string GuardianName { get; set; } = string.Empty;

    public string GuardianPhone { get; set; } = string.Empty;
}

public class DashboardSummaryDto
{
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public int AdmittedStudents { get; set; }
    public int ProbationStudents { get; set; }
    public int ProgramCount { get; set; }
    public bool IsUsingMongoDb { get; set; }
    public string DatabaseType => IsUsingMongoDb ? "MongoDB (Local Server localhost:27017)" : "In-Memory Store (Resilient Fallback)";
    public List<ProgramStatDto> ProgramBreakdown { get; set; } = new();
    public List<Student> RecentAdmissions { get; set; } = new();
}

public class ProgramStatDto
{
    public string Program { get; set; } = string.Empty;
    public int Count { get; set; }
    public int ActiveCount { get; set; }
}
