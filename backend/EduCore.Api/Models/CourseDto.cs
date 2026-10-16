using System.ComponentModel.DataAnnotations;

namespace EduCore.Api.Models;

public class CreateCourseDto
{
    [Required(ErrorMessage = "Course code is required")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Course code must be between 3 and 20 characters")]
    [RegularExpression(@"^[A-Za-z0-9\.\-\s]+$", ErrorMessage = "Course code contains invalid characters")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course title is required")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "Course title must be between 3 and 120 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Credits is required")]
    [Range(1, 8, ErrorMessage = "Credits must be between 1 and 8")]
    public int Credits { get; set; } = 4;

    [Required(ErrorMessage = "Associated program is required")]
    public string ProgramCode { get; set; } = "BCA";

    [Required(ErrorMessage = "Semester is required")]
    [Range(1, 8, ErrorMessage = "Semester must be between 1 and 8")]
    public int Semester { get; set; } = 1;

    public string Department { get; set; } = "Computer Applications";

    public string CourseType { get; set; } = "CORE";

    public string Description { get; set; } = string.Empty;
}

public class UpdateCourseDto
{
    [Required(ErrorMessage = "Course code is required")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Course code must be between 3 and 20 characters")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course title is required")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "Course title must be between 3 and 120 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Credits is required")]
    [Range(1, 8, ErrorMessage = "Credits must be between 1 and 8")]
    public int Credits { get; set; } = 4;

    [Required(ErrorMessage = "Associated program is required")]
    public string ProgramCode { get; set; } = "BCA";

    [Required(ErrorMessage = "Semester is required")]
    [Range(1, 8, ErrorMessage = "Semester must be between 1 and 8")]
    public int Semester { get; set; } = 1;

    public string Department { get; set; } = "Computer Applications";

    public string CourseType { get; set; } = "CORE";

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = "ACTIVE";
}

public class ProgramDetailDto
{
    public ProgramInfo Program { get; set; } = new();
    public int CourseCount { get; set; }
    public int TotalCredits { get; set; }
    public List<Course> Courses { get; set; } = new();
}
