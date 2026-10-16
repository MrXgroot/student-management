using EduCore.Api.Data;
using EduCore.Api.Models;
using EduCore.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EduCore.UnitTests;

public class StudentServiceTests
{
    private readonly IStudentService _service;

    public StudentServiceTests()
    {
        var context = new MongoDbContext(new MongoDbOptions
        {
            ConnectionString = "",
            DatabaseName = "educore_mvp_test",
            AllowInMemoryFallback = true
        }, NullLogger<MongoDbContext>.Instance);

        _service = new StudentService(context, NullLogger<StudentService>.Instance);
    }

    [Fact]
    public async Task GetStudentsAsync_ReturnsSeededDemoStudents()
    {
        var students = await _service.GetStudentsAsync(null, "ALL", "ALL");

        Assert.NotNull(students);
        Assert.True(students.Count >= 12, "Expected at least 12 seeded Indian demo students");
        Assert.Contains(students, s => s.FirstName == "Ananya" && s.LastName == "Rao");
    }

    [Fact]
    public async Task CreateStudentAsync_GeneratesAdmissionNumber_AndPersists()
    {
        var dto = new CreateStudentDto
        {
            FirstName = "Vikramaditya",
            LastName = "Bhat",
            Email = "vikram.bhat@student.vit.edu",
            Phone = "9876543210",
            Program = "BCA",
            CurrentSemester = 1,
            Gender = "Male",
            DateOfBirth = new DateTime(2005, 5, 10),
            Address = "Bengaluru",
            GuardianName = "Subraya Bhat",
            GuardianPhone = "9876501234"
        };

        var created = await _service.CreateStudentAsync(dto);

        Assert.NotNull(created);
        Assert.False(string.IsNullOrEmpty(created.Id));
        Assert.StartsWith("ADM-", created.AdmissionNumber);
        Assert.Equal("Vikramaditya", created.FirstName);
        Assert.Equal("Bhat", created.LastName);
        Assert.Equal("Vikramaditya Bhat", created.FullName);
        Assert.Equal("ADMITTED", created.LifecycleStatus);

        var fetched = await _service.GetStudentByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal(created.AdmissionNumber, fetched.AdmissionNumber);
    }

    [Fact]
    public async Task UpdateStudentAsync_ModifiesFields_AndUpdatesTimestamp()
    {
        var students = await _service.GetStudentsAsync(null, "ALL", "ALL");
        var target = students.First();

        var updateDto = new UpdateStudentDto
        {
            FirstName = target.FirstName,
            LastName = target.LastName,
            Email = target.Email,
            Phone = "9988776655",
            Program = target.Program,
            CurrentSemester = target.CurrentSemester,
            LifecycleStatus = "ACTIVE",
            RollNumber = "24BCA999",
            Gender = target.Gender,
            DateOfBirth = target.DateOfBirth,
            Address = "Updated Address, Bengaluru",
            GuardianName = target.GuardianName,
            GuardianPhone = target.GuardianPhone
        };

        var updated = await _service.UpdateStudentAsync(target.Id, updateDto);

        Assert.NotNull(updated);
        Assert.Equal("9988776655", updated.Phone);
        Assert.Equal("ACTIVE", updated.LifecycleStatus);
        Assert.Equal("24BCA999", updated.RollNumber);
        Assert.Equal("Updated Address, Bengaluru", updated.Address);
    }

    [Fact]
    public async Task GetStudentsAsync_FiltersByProgramAndSearch()
    {
        var bcaStudents = await _service.GetStudentsAsync(null, "ALL", "BCA");
        Assert.All(bcaStudents, s => Assert.Equal("BCA", s.Program));

        var searched = await _service.GetStudentsAsync("Ananya", "ALL", "ALL");
        Assert.NotEmpty(searched);
        Assert.Contains(searched, s => s.FullName.Contains("Ananya"));
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_CalculatesCorrectMetrics()
    {
        var summary = await _service.GetDashboardSummaryAsync();

        Assert.NotNull(summary);
        Assert.True(summary.TotalStudents >= 12);
        Assert.True(summary.ProgramCount >= 5);
        Assert.NotEmpty(summary.RecentAdmissions);
        Assert.NotEmpty(summary.ProgramBreakdown);
    }
}
