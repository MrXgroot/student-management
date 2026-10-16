using EduCore.Api.Data;
using EduCore.Api.Models;
using EduCore.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EduCore.UnitTests;

public class AcademicServiceTests
{
    private readonly IAcademicService _service;

    public AcademicServiceTests()
    {
        var context = new MongoDbContext(new MongoDbOptions
        {
            ConnectionString = "",
            DatabaseName = "educore_mvp_test",
            AllowInMemoryFallback = true
        }, NullLogger<MongoDbContext>.Instance);

        _service = new AcademicService(context, NullLogger<AcademicService>.Instance);
    }

    [Fact]
    public async Task GetProgramsAsync_ReturnsAllConfiguredPrograms()
    {
        var programs = await _service.GetProgramsAsync();

        Assert.NotNull(programs);
        Assert.Equal(6, programs.Count);
        Assert.Contains(programs, p => p.Code == "BCA");
        Assert.Contains(programs, p => p.Code == "B.Sc. Computer Science");
        Assert.Contains(programs, p => p.Code == "B.Com.");
        Assert.Contains(programs, p => p.Code == "BBA");
        Assert.Contains(programs, p => p.Code == "MCA");
        Assert.Contains(programs, p => p.Code == "MBA");
    }

    [Fact]
    public async Task GetProgramDetailsAsync_ReturnsCurriculumAndCredits()
    {
        var detail = await _service.GetProgramDetailsAsync("BCA");

        Assert.NotNull(detail);
        Assert.Equal("BCA", detail.Program.Code);
        Assert.True(detail.CourseCount >= 5, "Expected at least 5 BCA courses seeded");
        Assert.True(detail.TotalCredits > 0);
        Assert.Contains(detail.Courses, c => c.Code == "BCA-101");
    }

    [Fact]
    public async Task GetAcademicPeriodsAsync_ReturnsPeriodsWithCurrentOddSemester()
    {
        var periods = await _service.GetAcademicPeriodsAsync();

        Assert.NotNull(periods);
        Assert.True(periods.Count >= 2);

        var current = await _service.GetCurrentAcademicPeriodAsync();
        Assert.NotNull(current);
        Assert.Equal("2026-2027", current.AcademicYear);
        Assert.Equal("2026-ODD", current.TermCode);
    }

    [Fact]
    public async Task GetCoursesAsync_FiltersByProgramAndSemester()
    {
        var sem1BcaCourses = await _service.GetCoursesAsync(null, "BCA", 1, "ALL");

        Assert.NotNull(sem1BcaCourses);
        Assert.NotEmpty(sem1BcaCourses);
        Assert.All(sem1BcaCourses, c =>
        {
            Assert.Equal("BCA", c.ProgramCode);
            Assert.Equal(1, c.Semester);
        });
    }

    [Fact]
    public async Task CreateCourseAsync_ValidDto_PersistsAndReturns()
    {
        var dto = new CreateCourseDto
        {
            Code = "BCA-402",
            Title = "Mobile Application Development with Flutter",
            Credits = 4,
            ProgramCode = "BCA",
            Semester = 4,
            Department = "Computer Applications",
            CourseType = "CORE",
            Description = "Cross-platform mobile apps, Dart language, and state management."
        };

        var created = await _service.CreateCourseAsync(dto);

        Assert.NotNull(created);
        Assert.False(string.IsNullOrEmpty(created.Id));
        Assert.Equal("BCA-402", created.Code);
        Assert.Equal("ACTIVE", created.Status);

        var fetched = await _service.GetCourseByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Mobile Application Development with Flutter", fetched.Title);

        // Cleanup
        await _service.DeleteCourseAsync(created.Id);
    }

    [Fact]
    public async Task CreateCourseAsync_DuplicateCode_ThrowsInvalidOperationException()
    {
        var dto = new CreateCourseDto
        {
            Code = "BCA-101", // Already exists in seeds
            Title = "Another C Course",
            Credits = 4,
            ProgramCode = "BCA",
            Semester = 1,
            Department = "Computer Applications",
            CourseType = "CORE",
            Description = "Duplicate test"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateCourseAsync(dto));
    }

    [Fact]
    public async Task UpdateCourseAsync_ModifiesCourseDetails()
    {
        var dto = new CreateCourseDto
        {
            Code = "BCA-999-TEMP",
            Title = "Temporary Elective Course",
            Credits = 3,
            ProgramCode = "BCA",
            Semester = 6,
            Department = "Computer Applications",
            CourseType = "ELECTIVE",
            Description = "Temporary"
        };

        var created = await _service.CreateCourseAsync(dto);

        var updateDto = new UpdateCourseDto
        {
            Code = "BCA-999-TEMP",
            Title = "Updated Advanced Elective Course",
            Credits = 4,
            ProgramCode = "BCA",
            Semester = 6,
            Department = "Computer Applications",
            CourseType = "ELECTIVE",
            Description = "Updated description",
            Status = "INACTIVE"
        };

        var updated = await _service.UpdateCourseAsync(created.Id, updateDto);

        Assert.NotNull(updated);
        Assert.Equal("Updated Advanced Elective Course", updated.Title);
        Assert.Equal(4, updated.Credits);
        Assert.Equal("INACTIVE", updated.Status);

        // Cleanup
        await _service.DeleteCourseAsync(created.Id);
    }
}
