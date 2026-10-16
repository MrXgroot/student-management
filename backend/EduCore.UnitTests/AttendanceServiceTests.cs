using EduCore.Api.Models;
using EduCore.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EduCore.UnitTests;

public class AttendanceServiceTests
{
    private readonly IStudentService _studentService;
    private readonly IAcademicService _academicService;
    private readonly ITermRegistrationService _registrationService;
    private readonly IAttendanceService _attendanceService;

    public AttendanceServiceTests()
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
        _attendanceService = new AttendanceService(config, _studentService, _academicService, _registrationService, NullLogger<AttendanceService>.Instance);
    }

    private async Task<Student> CreateTestStudentAsync(string program = "BCA", int semester = 1)
    {
        var unique = Guid.NewGuid().ToString("N")[..6];
        var dto = new CreateStudentDto
        {
            FirstName = "AttStudent" + unique,
            LastName = "Test",
            Email = $"att.{unique}@student.vit.edu",
            Phone = "98" + new Random().Next(10000000, 99999999),
            Program = program,
            CurrentSemester = semester,
            Gender = "Other"
        };
        return await _studentService.CreateStudentAsync(dto);
    }

    private async Task<(Student student, Course course, TermRegistration reg)> SetupEnrolledStudentAndCourseAsync()
    {
        var student = await CreateTestStudentAsync("BCA", 1);
        var courses = await _academicService.GetCoursesAsync();
        var course = courses.First(c => c.ProgramCode == "BCA" && c.Semester == 1);

        var reg = await _registrationService.RegisterStudentForTermAsync(new CreateTermRegistrationDto
        {
            StudentId = student.Id,
            AcademicPeriodCode = "2026-ODD",
            Semester = 1
        });

        await _registrationService.EnrollCourseAsync(reg.Id, course.Id);
        return (student, course, reg);
    }

    [Fact]
    public async Task GetAttendanceSheetAsync_ValidCourse_ReturnsEnrolledStudent()
    {
        var (student, course, _) = await SetupEnrolledStudentAndCourseAsync();
        var targetDate = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);

        var sheet = await _attendanceService.GetAttendanceSheetAsync(course.Id, targetDate, "2026-ODD");

        Assert.NotNull(sheet);
        Assert.Equal(course.Code, sheet.CourseCode);
        Assert.Contains(sheet.Students, s => s.StudentId == student.Id);
    }

    [Fact]
    public async Task SaveAttendanceAsync_ValidBatch_RecordsAttendanceSuccessfully()
    {
        var (student, course, _) = await SetupEnrolledStudentAndCourseAsync();
        var targetDate = new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc);

        var batchDto = new SaveAttendanceBatchDto
        {
            AcademicPeriodCode = "2026-ODD",
            CourseId = course.Id,
            ClassDate = targetDate,
            Entries = new List<AttendanceEntryDto>
            {
                new AttendanceEntryDto
                {
                    StudentId = student.Id,
                    Status = "PRESENT",
                    Remarks = "On time"
                }
            }
        };

        var savedSheet = await _attendanceService.SaveAttendanceAsync(batchDto);

        Assert.NotNull(savedSheet);
        Assert.True(savedSheet.IsRecorded);
        Assert.True(savedSheet.PresentCount >= 1);

        var studentItem = savedSheet.Students.FirstOrDefault(s => s.StudentId == student.Id);
        Assert.NotNull(studentItem);
        Assert.Equal("PRESENT", studentItem.Status);
        Assert.Equal("On time", studentItem.Remarks);
    }

    [Fact]
    public async Task SaveAttendanceAsync_SubsequentSubmission_UpdatesAttendanceWithoutDuplicates()
    {
        var (student, course, _) = await SetupEnrolledStudentAndCourseAsync();
        var targetDate = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

        // First submission: PRESENT
        await _attendanceService.SaveAttendanceAsync(new SaveAttendanceBatchDto
        {
            AcademicPeriodCode = "2026-ODD",
            CourseId = course.Id,
            ClassDate = targetDate,
            Entries = new List<AttendanceEntryDto>
            {
                new AttendanceEntryDto { StudentId = student.Id, Status = "PRESENT" }
            }
        });

        // Second submission: edit to ABSENT with remark
        var updatedSheet = await _attendanceService.SaveAttendanceAsync(new SaveAttendanceBatchDto
        {
            AcademicPeriodCode = "2026-ODD",
            CourseId = course.Id,
            ClassDate = targetDate,
            Entries = new List<AttendanceEntryDto>
            {
                new AttendanceEntryDto { StudentId = student.Id, Status = "ABSENT", Remarks = "Late sickness note" }
            }
        });

        var studentItem = updatedSheet.Students.FirstOrDefault(s => s.StudentId == student.Id);
        Assert.NotNull(studentItem);
        Assert.Equal("ABSENT", studentItem.Status);
        Assert.Equal("Late sickness note", studentItem.Remarks);

        // Verify history shows only 1 session for this date
        var history = await _attendanceService.GetAttendanceHistoryAsync(course.Id, "2026-ODD");
        var sessionForDate = history.Where(h => h.ClassDateString == "2026-09-14").ToList();
        Assert.Single(sessionForDate);
    }

    [Fact]
    public async Task SaveAttendanceAsync_NormalizesDateToMidnightUtc()
    {
        var (student, course, _) = await SetupEnrolledStudentAndCourseAsync();
        // Pass a date with arbitrary time of day
        var dateWithTime = new DateTime(2026, 9, 20, 15, 45, 30, DateTimeKind.Utc);

        var sheet = await _attendanceService.SaveAttendanceAsync(new SaveAttendanceBatchDto
        {
            AcademicPeriodCode = "2026-ODD",
            CourseId = course.Id,
            ClassDate = dateWithTime,
            Entries = new List<AttendanceEntryDto>
            {
                new AttendanceEntryDto { StudentId = student.Id, Status = "PRESENT" }
            }
        });

        Assert.Equal(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc), sheet.ClassDate);
        Assert.Equal("2026-09-20", sheet.ClassDateString);
    }

    [Fact]
    public async Task GetAttendanceHistoryAsync_MultipleSessions_ReturnsGroupedSessions()
    {
        var (student, course, _) = await SetupEnrolledStudentAndCourseAsync();
        var date1 = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc);
        var date2 = new DateTime(2026, 8, 6, 0, 0, 0, DateTimeKind.Utc);

        await _attendanceService.SaveAttendanceAsync(new SaveAttendanceBatchDto
        {
            AcademicPeriodCode = "2026-ODD",
            CourseId = course.Id,
            ClassDate = date1,
            Entries = new List<AttendanceEntryDto>
            {
                new AttendanceEntryDto { StudentId = student.Id, Status = "PRESENT" }
            }
        });

        await _attendanceService.SaveAttendanceAsync(new SaveAttendanceBatchDto
        {
            AcademicPeriodCode = "2026-ODD",
            CourseId = course.Id,
            ClassDate = date2,
            Entries = new List<AttendanceEntryDto>
            {
                new AttendanceEntryDto { StudentId = student.Id, Status = "ABSENT" }
            }
        });

        var history = await _attendanceService.GetAttendanceHistoryAsync(course.Id, "2026-ODD");
        Assert.True(history.Count >= 2);
        // Verify ordering: newest first
        var idxDate2 = history.FindIndex(h => h.ClassDateString == "2026-08-06");
        var idxDate1 = history.FindIndex(h => h.ClassDateString == "2026-08-05");
        Assert.True(idxDate2 < idxDate1);
    }

    [Fact]
    public async Task GetStudentAttendanceHistoryAsync_ReturnsRecordsForStudent()
    {
        var (student, course, _) = await SetupEnrolledStudentAndCourseAsync();
        var date = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);

        await _attendanceService.SaveAttendanceAsync(new SaveAttendanceBatchDto
        {
            AcademicPeriodCode = "2026-ODD",
            CourseId = course.Id,
            ClassDate = date,
            Entries = new List<AttendanceEntryDto>
            {
                new AttendanceEntryDto { StudentId = student.Id, Status = "PRESENT" }
            }
        });

        var studentRecords = await _attendanceService.GetStudentAttendanceHistoryAsync(student.Id, course.Id);
        Assert.Contains(studentRecords, r => r.ClassDateString == "2026-08-10" && r.Status == "PRESENT");
    }

    [Fact]
    public async Task SaveAttendanceAsync_InvalidCourse_ThrowsKeyNotFoundException()
    {
        var batch = new SaveAttendanceBatchDto
        {
            AcademicPeriodCode = "2026-ODD",
            CourseId = "non_existent_course_id_12345",
            ClassDate = DateTime.UtcNow.Date,
            Entries = new List<AttendanceEntryDto>
            {
                new AttendanceEntryDto { StudentId = "some_student", Status = "PRESENT" }
            }
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _attendanceService.SaveAttendanceAsync(batch));
    }

    [Fact]
    public async Task SaveAttendanceAsync_EmptyEntries_ThrowsArgumentException()
    {
        var courses = await _academicService.GetCoursesAsync();
        var course = courses.First();

        var batch = new SaveAttendanceBatchDto
        {
            AcademicPeriodCode = "2026-ODD",
            CourseId = course.Id,
            ClassDate = DateTime.UtcNow.Date,
            Entries = new List<AttendanceEntryDto>()
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _attendanceService.SaveAttendanceAsync(batch));
    }
}
