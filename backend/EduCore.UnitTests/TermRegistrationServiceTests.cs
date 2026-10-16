using EduCore.Api.Models;
using EduCore.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EduCore.UnitTests;

public class TermRegistrationServiceTests
{
    private readonly IStudentService _studentService;
    private readonly IAcademicService _academicService;
    private readonly ITermRegistrationService _registrationService;

    public TermRegistrationServiceTests()
    {
        var testConfig = new Dictionary<string, string?>
        {
            {"ConnectionStrings:MongoDb", "mongodb://localhost:27017"},
            {"DatabaseName", "educore_mvp_test"}
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(testConfig)
            .Build();

        _studentService = new StudentService(config, NullLogger<StudentService>.Instance);
        _academicService = new AcademicService(config, NullLogger<AcademicService>.Instance);
        _registrationService = new TermRegistrationService(config, _studentService, _academicService, NullLogger<TermRegistrationService>.Instance);
    }

    private async Task<Student> CreateTestStudentAsync(string program = "BCA", int semester = 1)
    {
        var unique = Guid.NewGuid().ToString("N")[..6];
        var dto = new CreateStudentDto
        {
            FirstName = "Student" + unique,
            LastName = "Test",
            Email = $"student.{unique}@student.vit.edu",
            Phone = "98" + new Random().Next(10000000, 99999999),
            Program = program,
            CurrentSemester = semester,
            Gender = "Other"
        };
        return await _studentService.CreateStudentAsync(dto);
    }

    [Fact]
    public async Task RegisterStudentForTermAsync_EligibleStudent_CreatesRegistration()
    {
        var student = await CreateTestStudentAsync("BCA", 1);

        var regDto = new CreateTermRegistrationDto
        {
            StudentId = student.Id,
            AcademicPeriodCode = "2026-ODD",
            Semester = 1
        };

        var registration = await _registrationService.RegisterStudentForTermAsync(regDto);

        Assert.NotNull(registration);
        Assert.False(string.IsNullOrEmpty(registration.Id));
        Assert.Equal(student.Id, registration.StudentId);
        Assert.Equal("BCA", registration.ProgramCode);
        Assert.Equal("2026-ODD", registration.AcademicPeriodCode);
        Assert.Equal(1, registration.Semester);
        Assert.Equal("REGISTERED", registration.Status);

        var updatedStudent = await _studentService.GetStudentByIdAsync(student.Id);
        Assert.NotNull(updatedStudent);
        Assert.Equal("ACTIVE", updatedStudent.LifecycleStatus);

        await _studentService.DeleteStudentAsync(student.Id);
    }

    [Fact]
    public async Task RegisterStudentForTermAsync_DuplicateRegistration_ThrowsInvalidOperationException()
    {
        var student = await CreateTestStudentAsync("BCA", 1);

        var regDto = new CreateTermRegistrationDto
        {
            StudentId = student.Id,
            AcademicPeriodCode = "2026-ODD",
            Semester = 1
        };

        await _registrationService.RegisterStudentForTermAsync(regDto);

        // Duplicate attempt
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _registrationService.RegisterStudentForTermAsync(regDto));

        await _studentService.DeleteStudentAsync(student.Id);
    }

    [Fact]
    public async Task RegisterStudentForTermAsync_IneligibleStudent_ThrowsInvalidOperationException()
    {
        var student = await CreateTestStudentAsync("BCA", 1);

        // Transition student to WITHDRAWN status
        await _studentService.UpdateStudentAsync(student.Id, new UpdateStudentDto
        {
            FirstName = student.FirstName,
            LastName = student.LastName,
            Email = student.Email,
            Phone = student.Phone,
            Program = student.Program,
            CurrentSemester = student.CurrentSemester,
            LifecycleStatus = "WITHDRAWN",
            Gender = student.Gender
        });

        var regDto = new CreateTermRegistrationDto
        {
            StudentId = student.Id,
            AcademicPeriodCode = "2026-ODD",
            Semester = 1
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _registrationService.RegisterStudentForTermAsync(regDto));

        Assert.Contains("WITHDRAWN", ex.Message);

        await _studentService.DeleteStudentAsync(student.Id);
    }

    [Fact]
    public async Task EnrollCourseAsync_ValidCourse_CreatesEnrollment()
    {
        var student = await CreateTestStudentAsync("BCA", 1);

        var registration = await _registrationService.RegisterStudentForTermAsync(new CreateTermRegistrationDto
        {
            StudentId = student.Id,
            AcademicPeriodCode = "2026-ODD",
            Semester = 1
        });

        var enrollment = await _registrationService.EnrollCourseAsync(registration.Id, "BCA-101");

        Assert.NotNull(enrollment);
        Assert.Equal("BCA-101", enrollment.CourseCode);
        Assert.Equal("ENROLLED", enrollment.Status);
        Assert.Equal(4, enrollment.Credits);

        var enrolledList = await _registrationService.GetEnrolledCoursesAsync(registration.Id);
        Assert.Contains(enrolledList, e => e.CourseCode == "BCA-101");

        await _studentService.DeleteStudentAsync(student.Id);
    }

    [Fact]
    public async Task EnrollCourseAsync_WrongProgram_ThrowsInvalidOperationException()
    {
        var student = await CreateTestStudentAsync("BCA", 1);

        var registration = await _registrationService.RegisterStudentForTermAsync(new CreateTermRegistrationDto
        {
            StudentId = student.Id,
            AcademicPeriodCode = "2026-ODD",
            Semester = 1
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _registrationService.EnrollCourseAsync(registration.Id, "MBA-101"));

        Assert.Contains("MBA", ex.Message);

        await _studentService.DeleteStudentAsync(student.Id);
    }

    [Fact]
    public async Task EnrollCourseAsync_WrongSemester_ThrowsInvalidOperationException()
    {
        var student = await CreateTestStudentAsync("BCA", 1);

        var registration = await _registrationService.RegisterStudentForTermAsync(new CreateTermRegistrationDto
        {
            StudentId = student.Id,
            AcademicPeriodCode = "2026-ODD",
            Semester = 1
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _registrationService.EnrollCourseAsync(registration.Id, "BCA-201"));

        Assert.Contains("Semester 2", ex.Message);

        await _studentService.DeleteStudentAsync(student.Id);
    }

    [Fact]
    public async Task EnrollCourseAsync_DuplicateEnrollment_ThrowsInvalidOperationException()
    {
        var student = await CreateTestStudentAsync("BCA", 1);

        var registration = await _registrationService.RegisterStudentForTermAsync(new CreateTermRegistrationDto
        {
            StudentId = student.Id,
            AcademicPeriodCode = "2026-ODD",
            Semester = 1
        });

        await _registrationService.EnrollCourseAsync(registration.Id, "BCA-101");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _registrationService.EnrollCourseAsync(registration.Id, "BCA-101"));

        await _studentService.DeleteStudentAsync(student.Id);
    }

    [Fact]
    public async Task EnrollCourseAsync_InactiveCourse_ThrowsInvalidOperationException()
    {
        var inactiveCode = "BCA-INACT-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        var inactiveCourse = await _academicService.CreateCourseAsync(new CreateCourseDto
        {
            Code = inactiveCode,
            Title = "Legacy Computing",
            Credits = 3,
            ProgramCode = "BCA",
            Semester = 1,
            Department = "Computer Applications"
        });

        await _academicService.UpdateCourseAsync(inactiveCourse.Id, new UpdateCourseDto
        {
            Code = inactiveCourse.Code,
            Title = inactiveCourse.Title,
            Credits = inactiveCourse.Credits,
            ProgramCode = inactiveCourse.ProgramCode,
            Semester = inactiveCourse.Semester,
            Department = inactiveCourse.Department,
            Status = "INACTIVE"
        });

        var student = await CreateTestStudentAsync("BCA", 1);
        var registration = await _registrationService.RegisterStudentForTermAsync(new CreateTermRegistrationDto
        {
            StudentId = student.Id,
            AcademicPeriodCode = "2026-ODD",
            Semester = 1
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _registrationService.EnrollCourseAsync(registration.Id, inactiveCourse.Id));

        Assert.Contains("INACTIVE", ex.Message);

        await _academicService.DeleteCourseAsync(inactiveCourse.Id);
        await _studentService.DeleteStudentAsync(student.Id);
    }

    [Fact]
    public async Task GetRegistrationsAsync_FiltersByPeriodAndProgram()
    {
        var student = await CreateTestStudentAsync("BCA", 1);
        await _registrationService.RegisterStudentForTermAsync(new CreateTermRegistrationDto
        {
            StudentId = student.Id,
            AcademicPeriodCode = "2026-ODD",
            Semester = 1
        });

        var filtered = await _registrationService.GetRegistrationsAsync("2026-ODD", "BCA", 1, "ALL", null);

        Assert.NotNull(filtered);
        Assert.NotEmpty(filtered);
        Assert.Contains(filtered, r => r.StudentId == student.Id);

        await _studentService.DeleteStudentAsync(student.Id);
    }

    [Fact]
    public async Task DropCourseEnrollmentAsync_RemovesEnrollment()
    {
        var student = await CreateTestStudentAsync("BCA", 1);
        var registration = await _registrationService.RegisterStudentForTermAsync(new CreateTermRegistrationDto
        {
            StudentId = student.Id,
            AcademicPeriodCode = "2026-ODD",
            Semester = 1
        });

        var enrollment = await _registrationService.EnrollCourseAsync(registration.Id, "BCA-102");
        var enrolledBefore = await _registrationService.GetEnrolledCoursesAsync(registration.Id);
        Assert.Contains(enrolledBefore, e => e.CourseCode == "BCA-102");

        var dropped = await _registrationService.DropCourseEnrollmentAsync(registration.Id, enrollment.CourseId);
        Assert.True(dropped);

        var enrolledAfter = await _registrationService.GetEnrolledCoursesAsync(registration.Id);
        Assert.DoesNotContain(enrolledAfter, e => e.CourseCode == "BCA-102");

        await _studentService.DeleteStudentAsync(student.Id);
    }
}
