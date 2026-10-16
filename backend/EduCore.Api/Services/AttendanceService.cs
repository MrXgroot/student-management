using System.Collections.Concurrent;
using EduCore.Api.Data;
using EduCore.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EduCore.Api.Services;

public class AttendanceService : IAttendanceService
{
    private readonly ILogger<AttendanceService> _logger;
    private readonly IStudentService _studentService;
    private readonly IAcademicService _academicService;
    private readonly ITermRegistrationService _registrationService;

    private readonly IMongoCollection<AttendanceRecord>? _collection;
    private readonly ConcurrentDictionary<string, AttendanceRecord> _inMemoryRecords = new();
    private readonly bool _allowInMemoryFallback;
    private bool _isMongoDbActive;

    public bool IsMongoDbActive => _isMongoDbActive;

    [ActivatorUtilitiesConstructor]
    public AttendanceService(
        IMongoDbContext dbContext,
        IStudentService studentService,
        IAcademicService academicService,
        ITermRegistrationService registrationService,
        ILogger<AttendanceService> logger)
    {
        _logger = logger;
        _studentService = studentService;
        _academicService = academicService;
        _registrationService = registrationService;
        _isMongoDbActive = dbContext.IsAvailable;
        _allowInMemoryFallback = dbContext.AllowInMemoryFallback;

        if (_isMongoDbActive && dbContext.Database != null)
        {
            try
            {
                _collection = dbContext.GetCollection<AttendanceRecord>("attendanceRecords");

                // Create Unique Compound Index: { StudentId: 1, CourseId: 1, ClassDate: 1 }
                var indexKeys = Builders<AttendanceRecord>.IndexKeys
                    .Ascending(r => r.StudentId)
                    .Ascending(r => r.CourseId)
                    .Ascending(r => r.ClassDate);

                _collection.Indexes.CreateOne(new CreateIndexModel<AttendanceRecord>(indexKeys, new CreateIndexOptions
                {
                    Name = "idx_attendance_student_course_date_unique",
                    Unique = true
                }));

                // Secondary Index for efficient date/course queries
                var courseDateKeys = Builders<AttendanceRecord>.IndexKeys
                    .Ascending(r => r.CourseId)
                    .Ascending(r => r.ClassDate);
                _collection.Indexes.CreateOne(new CreateIndexModel<AttendanceRecord>(courseDateKeys, new CreateIndexOptions
                {
                    Name = "idx_attendance_course_date"
                }));
            }
            catch (Exception ex)
            {
                _isMongoDbActive = false;
                _collection = null;
                _logger.LogWarning(ex, "Error initializing Attendance collection/indexes in MongoDB. Using resilient in-memory storage.");
            }
        }

        // Initialize demo enrollments and seed attendance record if needed
        InitializeDemoAttendanceAsync().GetAwaiter().GetResult();
    }


    private async Task InitializeDemoAttendanceAsync()
    {
        if (!_allowInMemoryFallback && !_isMongoDbActive)
        {
            _logger.LogWarning("MongoDB is not active and in-memory fallback is disabled in this environment. In-memory demo attendance will not be initialized.");
            return;
        }

        try
        {
            var courses = await _academicService.GetCoursesAsync();
            var bcaCourse = courses.FirstOrDefault(c => c.Code.Equals("BCA-101", StringComparison.OrdinalIgnoreCase))
                         ?? courses.FirstOrDefault();

            if (bcaCourse == null) return;

            var existingEnrollments = await _registrationService.GetEnrollmentsByCourseAsync(bcaCourse.Id, "2026-ODD");
            if (existingEnrollments.Count == 0)
            {
                // Ensure 3 BCA students are registered and enrolled so teachers have realistic data
                var students = await _studentService.GetStudentsAsync();
                var bcaStudents = students.Where(s => s.Program.Equals("BCA", StringComparison.OrdinalIgnoreCase)).Take(3).ToList();

                foreach (var s in bcaStudents)
                {
                    try
                    {
                        var reg = await _registrationService.RegisterStudentForTermAsync(new CreateTermRegistrationDto
                        {
                            StudentId = s.Id,
                            AcademicPeriodCode = "2026-ODD",
                            Semester = 1
                        });
                        await _registrationService.EnrollCourseAsync(reg.Id, bcaCourse.Id);
                    }
                    catch
                    {
                        // Already registered or enrolled
                    }
                }
            }

            // Seed a previous attendance record for yesterday if no attendance exists
            var history = await GetAttendanceHistoryAsync(bcaCourse.Id, "2026-ODD");
            if (history.Count == 0)
            {
                var enrollments = await _registrationService.GetEnrollmentsByCourseAsync(bcaCourse.Id, "2026-ODD");
                if (enrollments.Count > 0)
                {
                    var yesterday = DateTime.UtcNow.Date.AddDays(-1);
                    var entries = enrollments.Select((e, idx) => new AttendanceEntryDto
                    {
                        StudentId = e.StudentId,
                        Status = idx % 3 == 2 ? "ABSENT" : "PRESENT",
                        Remarks = idx % 3 == 2 ? "Excused medical leave" : null
                    }).ToList();

                    await SaveAttendanceAsync(new SaveAttendanceBatchDto
                    {
                        AcademicPeriodCode = "2026-ODD",
                        CourseId = bcaCourse.Id,
                        ClassDate = yesterday,
                        Entries = entries
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize demo attendance seed.");
        }
    }

    private async Task<Course> ResolveCourseAsync(string courseIdOrCode)
    {
        var course = await _academicService.GetCourseByIdAsync(courseIdOrCode);
        if (course != null) return course;

        var allCourses = await _academicService.GetCoursesAsync();
        course = allCourses.FirstOrDefault(c => c.Code.Equals(courseIdOrCode, StringComparison.OrdinalIgnoreCase));
        if (course != null) return course;

        throw new KeyNotFoundException($"Course '{courseIdOrCode}' not found.");
    }

    public async Task<CourseAttendanceSheetDto> GetAttendanceSheetAsync(string courseId, DateTime classDate, string? academicPeriodCode = null)
    {
        var course = await ResolveCourseAsync(courseId);
        var period = string.IsNullOrWhiteSpace(academicPeriodCode) ? "2026-ODD" : academicPeriodCode;
        var normalizedDate = classDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(classDate.Date, DateTimeKind.Utc)
            : classDate.ToUniversalTime().Date;

        var enrollments = await _registrationService.GetEnrollmentsByCourseAsync(course.Id, period);

        // Fetch existing attendance records for this course and date
        var existingRecords = new List<AttendanceRecord>();
        if (_isMongoDbActive && _collection != null)
        {
            try
            {
                var filter = Builders<AttendanceRecord>.Filter.Eq(r => r.CourseId, course.Id)
                           & Builders<AttendanceRecord>.Filter.Eq(r => r.ClassDate, normalizedDate);
                existingRecords = await _collection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query MongoDB attendance records for {Course} {Date}", course.Code, normalizedDate);
                existingRecords = _inMemoryRecords.Values
                    .Where(r => r.CourseId == course.Id && r.ClassDate == normalizedDate)
                    .ToList();
            }
        }
        else
        {
            existingRecords = _inMemoryRecords.Values
                .Where(r => r.CourseId == course.Id && r.ClassDate == normalizedDate)
                .ToList();
        }

        var recordMap = existingRecords.ToDictionary(r => r.StudentId, r => r);

        var sheetItems = new List<StudentAttendanceItemDto>();
        foreach (var enrollment in enrollments)
        {
            var student = await _studentService.GetStudentByIdAsync(enrollment.StudentId);
            recordMap.TryGetValue(enrollment.StudentId, out var rec);

            sheetItems.Add(new StudentAttendanceItemDto
            {
                StudentId = enrollment.StudentId,
                AdmissionNumber = student?.AdmissionNumber ?? string.Empty,
                StudentName = student?.FullName ?? enrollment.StudentId,
                RollNumber = student?.RollNumber,
                ProgramCode = student?.Program ?? course.ProgramCode,
                CurrentSemester = student?.CurrentSemester ?? course.Semester,
                AttendanceRecordId = rec?.Id,
                Status = rec?.Status,
                Remarks = rec?.Remarks,
                UpdatedAt = rec?.UpdatedAt
            });
        }

        // Sort students alphabetically by name
        sheetItems = sheetItems.OrderBy(s => s.StudentName).ToList();

        var isRecorded = sheetItems.Any(s => s.AttendanceRecordId != null);
        var presentCount = sheetItems.Count(s => string.Equals(s.Status, "PRESENT", StringComparison.OrdinalIgnoreCase));
        var absentCount = sheetItems.Count(s => string.Equals(s.Status, "ABSENT", StringComparison.OrdinalIgnoreCase));

        return new CourseAttendanceSheetDto
        {
            AcademicPeriodCode = period,
            CourseId = course.Id,
            CourseCode = course.Code,
            CourseTitle = course.Title,
            ClassDate = normalizedDate,
            ClassDateString = normalizedDate.ToString("yyyy-MM-dd"),
            IsRecorded = isRecorded,
            TotalEnrolled = sheetItems.Count,
            PresentCount = presentCount,
            AbsentCount = absentCount,
            Students = sheetItems
        };
    }

    public async Task<CourseAttendanceSheetDto> SaveAttendanceAsync(SaveAttendanceBatchDto dto)
    {
        if (dto.Entries == null || dto.Entries.Count == 0)
        {
            throw new ArgumentException("Attendance entries list cannot be empty.");
        }

        var course = await ResolveCourseAsync(dto.CourseId);
        var period = string.IsNullOrWhiteSpace(dto.AcademicPeriodCode) ? "2026-ODD" : dto.AcademicPeriodCode;
        var normalizedDate = dto.ClassDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dto.ClassDate.Date, DateTimeKind.Utc)
            : dto.ClassDate.ToUniversalTime().Date;

        var now = DateTime.UtcNow;

        foreach (var entry in dto.Entries)
        {
            var student = await _studentService.GetStudentByIdAsync(entry.StudentId);
            if (student == null) continue;

            var status = entry.Status.Trim().ToUpperInvariant();
            if (status != "PRESENT" && status != "ABSENT")
            {
                status = "PRESENT";
            }

            var record = new AttendanceRecord
            {
                AcademicPeriodCode = period,
                CourseId = course.Id,
                CourseCode = course.Code,
                CourseTitle = course.Title,
                StudentId = student.Id,
                AdmissionNumber = student.AdmissionNumber,
                StudentName = student.FullName,
                RollNumber = student.RollNumber,
                ClassDate = normalizedDate,
                ClassDateString = normalizedDate.ToString("yyyy-MM-dd"),
                Status = status,
                Remarks = entry.Remarks,
                UpdatedAt = now
            };

            var memoryKey = $"{student.Id}_{course.Id}_{normalizedDate:yyyy-MM-dd}";

            if (_isMongoDbActive && _collection != null)
            {
                try
                {
                    var filter = Builders<AttendanceRecord>.Filter.Eq(r => r.StudentId, student.Id)
                               & Builders<AttendanceRecord>.Filter.Eq(r => r.CourseId, course.Id)
                               & Builders<AttendanceRecord>.Filter.Eq(r => r.ClassDate, normalizedDate);

                    var existing = await _collection.Find(filter).FirstOrDefaultAsync();
                    if (existing != null)
                    {
                        record.Id = existing.Id;
                        record.CreatedAt = existing.CreatedAt;
                    }
                    else
                    {
                        record.CreatedAt = now;
                    }

                    await _collection.ReplaceOneAsync(filter, record, new ReplaceOptions { IsUpsert = true });
                    _inMemoryRecords[memoryKey] = record;
                }
                catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
                {
                    _logger.LogWarning("Concurrent attendance write detected for {Student} {Course} {Date}. Retrying upsert.", student.Id, course.Id, normalizedDate);
                    var filter = Builders<AttendanceRecord>.Filter.Eq(r => r.StudentId, student.Id)
                               & Builders<AttendanceRecord>.Filter.Eq(r => r.CourseId, course.Id)
                               & Builders<AttendanceRecord>.Filter.Eq(r => r.ClassDate, normalizedDate);
                    await _collection.ReplaceOneAsync(filter, record, new ReplaceOptions { IsUpsert = true });
                    _inMemoryRecords[memoryKey] = record;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "MongoDB write failed in SaveAttendanceAsync, fallback to in-memory.");
                    _inMemoryRecords[memoryKey] = record;
                }
            }
            else
            {
                if (_inMemoryRecords.TryGetValue(memoryKey, out var existingMem))
                {
                    record.Id = existingMem.Id;
                    record.CreatedAt = existingMem.CreatedAt;
                }
                else
                {
                    record.CreatedAt = now;
                }
                _inMemoryRecords[memoryKey] = record;
            }
        }

        return await GetAttendanceSheetAsync(course.Id, normalizedDate, period);
    }

    public async Task<List<AttendanceSessionSummaryDto>> GetAttendanceHistoryAsync(string courseId, string? academicPeriodCode = null)
    {
        var course = await ResolveCourseAsync(courseId);
        var period = string.IsNullOrWhiteSpace(academicPeriodCode) ? "2026-ODD" : academicPeriodCode;

        List<AttendanceRecord> records;
        if (_isMongoDbActive && _collection != null)
        {
            try
            {
                var builder = Builders<AttendanceRecord>.Filter;
                var filter = builder.Eq(r => r.CourseId, course.Id);
                if (!string.IsNullOrWhiteSpace(period) && !period.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    filter &= builder.Eq(r => r.AcademicPeriodCode, period);
                }

                records = await _collection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB lookup failed for attendance history.");
                records = _inMemoryRecords.Values.Where(r => r.CourseId == course.Id).ToList();
            }
        }
        else
        {
            records = _inMemoryRecords.Values.Where(r => r.CourseId == course.Id).ToList();
        }

        var summaries = records
            .GroupBy(r => r.ClassDateString)
            .Select(g =>
            {
                var first = g.First();
                return new AttendanceSessionSummaryDto
                {
                    CourseId = course.Id,
                    CourseCode = course.Code,
                    AcademicPeriodCode = first.AcademicPeriodCode,
                    ClassDate = first.ClassDate,
                    ClassDateString = g.Key,
                    TotalCount = g.Count(),
                    PresentCount = g.Count(r => r.Status.Equals("PRESENT", StringComparison.OrdinalIgnoreCase)),
                    AbsentCount = g.Count(r => r.Status.Equals("ABSENT", StringComparison.OrdinalIgnoreCase)),
                    UpdatedAt = g.Max(r => r.UpdatedAt)
                };
            })
            .OrderByDescending(s => s.ClassDate)
            .ToList();

        return summaries;
    }

    public async Task<List<AttendanceRecord>> GetStudentAttendanceHistoryAsync(string studentId, string? courseId = null, string? academicPeriodCode = null)
    {
        if (_isMongoDbActive && _collection != null)
        {
            try
            {
                var builder = Builders<AttendanceRecord>.Filter;
                var filter = builder.Eq(r => r.StudentId, studentId);
                if (!string.IsNullOrWhiteSpace(courseId) && !courseId.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    filter &= builder.Eq(r => r.CourseId, courseId);
                }
                if (!string.IsNullOrWhiteSpace(academicPeriodCode) && !academicPeriodCode.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    filter &= builder.Eq(r => r.AcademicPeriodCode, academicPeriodCode);
                }

                return await _collection.Find(filter).SortByDescending(r => r.ClassDate).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB student attendance history lookup failed.");
            }
        }

        var q = _inMemoryRecords.Values.Where(r => r.StudentId == studentId);
        if (!string.IsNullOrWhiteSpace(courseId) && !courseId.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(r => r.CourseId == courseId);
        }
        if (!string.IsNullOrWhiteSpace(academicPeriodCode) && !academicPeriodCode.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(r => r.AcademicPeriodCode.Equals(academicPeriodCode, StringComparison.OrdinalIgnoreCase));
        }

        return q.OrderByDescending(r => r.ClassDate).ToList();
    }
}
