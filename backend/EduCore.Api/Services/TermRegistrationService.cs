using System.Collections.Concurrent;
using EduCore.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EduCore.Api.Services;

public class TermRegistrationService : ITermRegistrationService
{
    private readonly ILogger<TermRegistrationService> _logger;
    private readonly IStudentService _studentService;
    private readonly IAcademicService _academicService;

    private readonly IMongoCollection<TermRegistration>? _regCollection;
    private readonly IMongoCollection<CourseEnrollment>? _enrollCollection;

    private readonly ConcurrentDictionary<string, TermRegistration> _inMemoryRegistrations = new();
    private readonly ConcurrentDictionary<string, CourseEnrollment> _inMemoryEnrollments = new();

    private bool _isMongoDbActive;

    public bool IsMongoDbActive => _isMongoDbActive;

    public TermRegistrationService(
        IConfiguration config,
        IStudentService studentService,
        IAcademicService academicService,
        ILogger<TermRegistrationService> logger)
    {
        _logger = logger;
        _studentService = studentService;
        _academicService = academicService;

        var connectionString = config.GetConnectionString("MongoDb") ?? "mongodb://localhost:27017";
        var databaseName = config["DatabaseName"] ?? "educore_mvp";

        try
        {
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);
            var client = new MongoClient(settings);
            var database = client.GetDatabase(databaseName);

            _regCollection = database.GetCollection<TermRegistration>("termRegistrations");
            _enrollCollection = database.GetCollection<CourseEnrollment>("courseEnrollments");

            database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(2500);
            _isMongoDbActive = true;
            _logger.LogInformation("TermRegistrationService connected to MongoDB at {Conn}, Database: {Db}", connectionString, databaseName);

            // Create Unique Compound Index: { StudentId: 1, AcademicPeriodCode: 1, Semester: 1 }
            var regIndexKeys = Builders<TermRegistration>.IndexKeys
                .Ascending(r => r.StudentId)
                .Ascending(r => r.AcademicPeriodCode)
                .Ascending(r => r.Semester);
            _regCollection.Indexes.CreateOne(new CreateIndexModel<TermRegistration>(regIndexKeys, new CreateIndexOptions { Unique = true }));

            // Create Unique Compound Index on Enrollments: { RegistrationId: 1, CourseId: 1 }
            var enrollIndexKeys = Builders<CourseEnrollment>.IndexKeys
                .Ascending(e => e.RegistrationId)
                .Ascending(e => e.CourseId);
            _enrollCollection.Indexes.CreateOne(new CreateIndexModel<CourseEnrollment>(enrollIndexKeys, new CreateIndexOptions { Unique = true }));
        }
        catch (Exception ex)
        {
            _isMongoDbActive = false;
            _regCollection = null;
            _enrollCollection = null;
            _logger.LogWarning(ex, "MongoDB unavailable for TermRegistrationService. Using resilient in-memory storage.");
        }
    }

    public async Task<List<TermRegistration>> GetRegistrationsAsync(string? period, string? program, int? semester, string? status, string? search)
    {
        if (_isMongoDbActive && _regCollection != null)
        {
            try
            {
                var builder = Builders<TermRegistration>.Filter;
                var filter = builder.Empty;

                if (!string.IsNullOrWhiteSpace(period) && !period.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    filter &= builder.Eq(r => r.AcademicPeriodCode, period);
                }

                if (!string.IsNullOrWhiteSpace(program) && !program.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    filter &= builder.Eq(r => r.ProgramCode, program);
                }

                if (semester.HasValue && semester.Value > 0)
                {
                    filter &= builder.Eq(r => r.Semester, semester.Value);
                }

                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    filter &= builder.Eq(r => r.Status, status.ToUpperInvariant());
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim();
                    var regex = new BsonRegularExpression(s, "i");
                    filter &= (builder.Regex(r => r.StudentName, regex) | builder.Regex(r => r.AdmissionNumber, regex));
                }

                return await _regCollection.Find(filter)
                    .SortByDescending(r => r.RegisteredAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB read failed for term registrations. Falling back to in-memory.");
            }
        }

        var query = _inMemoryRegistrations.Values.AsQueryable();

        if (!string.IsNullOrWhiteSpace(period) && !period.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.AcademicPeriodCode.Equals(period, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(program) && !program.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.ProgramCode.Equals(program, StringComparison.OrdinalIgnoreCase));
        }

        if (semester.HasValue && semester.Value > 0)
        {
            query = query.Where(r => r.Semester == semester.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(r => r.StudentName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                                     r.AdmissionNumber.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderByDescending(r => r.RegisteredAt).ToList();
    }

    public async Task<TermRegistration?> GetRegistrationByIdAsync(string id)
    {
        if (_isMongoDbActive && _regCollection != null)
        {
            try
            {
                var reg = await _regCollection.Find(r => r.Id == id).FirstOrDefaultAsync();
                if (reg != null) return reg;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB read failed for registration {Id}.", id);
            }
        }

        _inMemoryRegistrations.TryGetValue(id, out var fallback);
        return fallback;
    }

    public async Task<List<TermRegistration>> GetRegistrationsByStudentIdAsync(string studentId)
    {
        if (_isMongoDbActive && _regCollection != null)
        {
            try
            {
                return await _regCollection.Find(r => r.StudentId == studentId)
                    .SortByDescending(r => r.RegisteredAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB read failed for student registrations {StudentId}.", studentId);
            }
        }

        return _inMemoryRegistrations.Values
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.RegisteredAt)
            .ToList();
    }

    public async Task<TermRegistrationDetailDto?> GetRegistrationDetailAsync(string id)
    {
        var registration = await GetRegistrationByIdAsync(id);
        if (registration == null) return null;

        var student = await _studentService.GetStudentByIdAsync(registration.StudentId);
        var enrolledCourses = await GetEnrolledCoursesAsync(id);

        return new TermRegistrationDetailDto
        {
            Registration = registration,
            Student = student,
            EnrolledCourses = enrolledCourses,
            TotalCredits = enrolledCourses.Where(e => e.Status == "ENROLLED").Sum(e => e.Credits)
        };
    }

    public async Task<List<StudentTermStatusDto>> GetStudentEligibilityOverviewAsync(string periodCode, string? program, int? semester, string? search)
    {
        var students = await _studentService.GetStudentsAsync(search, "ALL", program);
        var registrations = await GetRegistrationsAsync(periodCode, program, semester, "ALL", null);
        var regMap = registrations.ToDictionary(r => r.StudentId);

        var result = new List<StudentTermStatusDto>();
        foreach (var s in students)
        {
            if (semester.HasValue && semester.Value > 0 && s.CurrentSemester != semester.Value)
            {
                continue;
            }

            regMap.TryGetValue(s.Id, out var reg);
            int enrolledCount = 0;
            int totalCredits = 0;

            if (reg != null)
            {
                var enrolled = await GetEnrolledCoursesAsync(reg.Id);
                var activeEnrolled = enrolled.Where(e => e.Status == "ENROLLED").ToList();
                enrolledCount = activeEnrolled.Count;
                totalCredits = activeEnrolled.Sum(e => e.Credits);
            }

            result.Add(new StudentTermStatusDto
            {
                Student = s,
                IsRegistered = reg != null,
                Registration = reg,
                EnrolledCourseCount = enrolledCount,
                TotalCredits = totalCredits
            });
        }

        return result.OrderBy(x => x.IsRegistered ? 0 : 1).ThenBy(x => x.Student.FullName).ToList();
    }

    public async Task<TermRegistration> RegisterStudentForTermAsync(CreateTermRegistrationDto dto)
    {
        // 1. Validate Student
        var student = await _studentService.GetStudentByIdAsync(dto.StudentId);
        if (student == null)
        {
            throw new KeyNotFoundException($"Student with ID '{dto.StudentId}' not found.");
        }

        // 2. Validate Student Eligibility
        var ineligibleStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "WITHDRAWN", "SUSPENDED", "DISCONTINUED", "GRADUATED", "INACTIVE"
        };

        if (ineligibleStatuses.Contains(student.LifecycleStatus))
        {
            throw new InvalidOperationException($"Student '{student.FullName}' is in status '{student.LifecycleStatus}' and is not eligible for term registration.");
        }

        // 3. Validate Program and Semester Range
        var program = await _academicService.GetProgramDetailsAsync(student.Program);
        if (program != null && (dto.Semester < 1 || dto.Semester > program.Program.TotalSemesters))
        {
            throw new InvalidOperationException($"Semester {dto.Semester} is outside the allowed range (1-{program.Program.TotalSemesters}) for program '{student.Program}'.");
        }

        // 4. Duplicate Registration Check
        var cleanPeriod = dto.AcademicPeriodCode.Trim().ToUpperInvariant();
        var existingRegs = await GetRegistrationsByStudentIdAsync(dto.StudentId);
        var duplicate = existingRegs.FirstOrDefault(r => 
            r.AcademicPeriodCode.Equals(cleanPeriod, StringComparison.OrdinalIgnoreCase) && 
            r.Semester == dto.Semester &&
            r.Status != "WITHDRAWN");

        if (duplicate != null)
        {
            throw new InvalidOperationException($"Student '{student.FullName}' is already registered for term {cleanPeriod} (Semester {dto.Semester}).");
        }

        // 5. Construct Registration
        var registration = new TermRegistration
        {
            Id = ObjectId.GenerateNewId().ToString(),
            StudentId = student.Id,
            AdmissionNumber = student.AdmissionNumber,
            StudentName = student.FullName,
            ProgramCode = student.Program,
            AcademicPeriodId = string.Empty,
            AcademicPeriodCode = cleanPeriod,
            Semester = dto.Semester,
            AcademicStanding = student.LifecycleStatus == "PROBATION" ? "PROBATION" : "REGULAR",
            Status = "REGISTERED",
            RegisteredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 6. Persist to MongoDB
        if (_isMongoDbActive && _regCollection != null)
        {
            try
            {
                await _regCollection.InsertOneAsync(registration);
                _logger.LogInformation("Registered student {AdmissionNumber} for term {Period} (Sem {Sem}) in MongoDB.",
                    student.AdmissionNumber, cleanPeriod, dto.Semester);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new InvalidOperationException($"Student '{student.FullName}' is already registered for term {cleanPeriod} (Semester {dto.Semester}).");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB write failed for term registration. Storing in in-memory fallback.");
            }
        }

        _inMemoryRegistrations[registration.Id] = registration;

        // 7. Transition Admitted Student to Active upon initial term registration
        if (student.LifecycleStatus.Equals("ADMITTED", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var updateDto = new UpdateStudentDto
                {
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    Email = student.Email,
                    Phone = student.Phone,
                    DateOfBirth = student.DateOfBirth,
                    Gender = student.Gender,
                    Program = student.Program,
                    CurrentSemester = dto.Semester,
                    LifecycleStatus = "ACTIVE",
                    RollNumber = string.IsNullOrWhiteSpace(student.RollNumber) 
                        ? $"26{student.Program.Replace(".", "").Replace(" ", "").ToUpperInvariant()}A{new Random().Next(10, 99)}"
                        : student.RollNumber,
                    Address = student.Address,
                    GuardianName = student.GuardianName,
                    GuardianPhone = student.GuardianPhone
                };
                await _studentService.UpdateStudentAsync(student.Id, updateDto);
                _logger.LogInformation("Transitioned student {AdmissionNumber} to ACTIVE on term registration.", student.AdmissionNumber);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-transition student status to ACTIVE.");
            }
        }

        return registration;
    }

    public async Task<TermRegistration?> UpdateRegistrationStatusAsync(string id, string newStatus)
    {
        var existing = await GetRegistrationByIdAsync(id);
        if (existing == null) return null;

        var cleanStatus = newStatus.Trim().ToUpperInvariant();
        if (cleanStatus != "REGISTERED" && cleanStatus != "CONFIRMED" && cleanStatus != "WITHDRAWN")
        {
            throw new ArgumentException("Status must be REGISTERED, CONFIRMED, or WITHDRAWN.");
        }

        existing.Status = cleanStatus;
        existing.UpdatedAt = DateTime.UtcNow;

        if (_isMongoDbActive && _regCollection != null)
        {
            try
            {
                var filter = Builders<TermRegistration>.Filter.Eq(r => r.Id, id);
                await _regCollection.ReplaceOneAsync(filter, existing);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB update failed for registration status {Id}.", id);
            }
        }

        _inMemoryRegistrations[id] = existing;
        return existing;
    }

    public async Task<List<CourseEnrollment>> GetEnrolledCoursesAsync(string registrationId)
    {
        if (_isMongoDbActive && _enrollCollection != null)
        {
            try
            {
                return await _enrollCollection.Find(e => e.RegistrationId == registrationId)
                    .SortBy(e => e.CourseCode)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB read failed for enrolled courses of registration {Id}.", registrationId);
            }
        }

        return _inMemoryEnrollments.Values
            .Where(e => e.RegistrationId == registrationId)
            .OrderBy(e => e.CourseCode)
            .ToList();
    }

    public async Task<List<Course>> GetEligibleCoursesAsync(string registrationId)
    {
        var registration = await GetRegistrationByIdAsync(registrationId);
        if (registration == null) return new List<Course>();

        var allProgramSemCourses = await _academicService.GetCoursesAsync(
            search: null,
            program: registration.ProgramCode,
            semester: registration.Semester,
            status: "ACTIVE"
        );

        var enrolled = await GetEnrolledCoursesAsync(registrationId);
        var enrolledCourseIds = enrolled
            .Where(e => e.Status == "ENROLLED")
            .Select(e => e.CourseId)
            .ToHashSet();

        return allProgramSemCourses
            .Where(c => !enrolledCourseIds.Contains(c.Id))
            .OrderBy(c => c.Code)
            .ToList();
    }

    public async Task<CourseEnrollment> EnrollCourseAsync(string registrationId, string courseIdOrCode)
    {
        // 1. Validate Registration
        var registration = await GetRegistrationByIdAsync(registrationId);
        if (registration == null)
        {
            throw new KeyNotFoundException($"Term registration with ID '{registrationId}' not found.");
        }

        if (registration.Status.Equals("WITHDRAWN", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot enroll courses in a withdrawn term registration.");
        }

        // 2. Validate Course
        var course = await _academicService.GetCourseByIdAsync(courseIdOrCode) ??
                     await _academicService.GetCourseByCodeAsync(courseIdOrCode);

        if (course == null)
        {
            throw new KeyNotFoundException($"Course '{courseIdOrCode}' was not found in the academic catalog.");
        }

        // Rule: Prevent enrollment into inactive courses
        if (!course.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Course '{course.Code}' is currently INACTIVE and not offered for enrollment.");
        }

        // Rule: Course must belong to student's program
        if (!course.ProgramCode.Equals(registration.ProgramCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Course '{course.Code}' belongs to '{course.ProgramCode}', but student is registered in '{registration.ProgramCode}'.");
        }

        // Rule: Course must match registration semester
        if (course.Semester != registration.Semester)
        {
            throw new InvalidOperationException($"Course '{course.Code}' is scheduled for Semester {course.Semester}, but student is registered for Semester {registration.Semester}.");
        }

        // Rule: Duplicate enrollment check
        var existingEnrollments = await GetEnrolledCoursesAsync(registrationId);
        var duplicate = existingEnrollments.FirstOrDefault(e => e.CourseId == course.Id && e.Status == "ENROLLED");
        if (duplicate != null)
        {
            throw new InvalidOperationException($"Student is already enrolled in course '{course.Code}' for this term registration.");
        }

        var enrollment = new CourseEnrollment
        {
            Id = ObjectId.GenerateNewId().ToString(),
            RegistrationId = registration.Id,
            StudentId = registration.StudentId,
            CourseId = course.Id,
            CourseCode = course.Code,
            CourseTitle = course.Title,
            Credits = course.Credits,
            CourseType = course.CourseType,
            AcademicPeriodCode = registration.AcademicPeriodCode,
            Status = "ENROLLED",
            EnrolledAt = DateTime.UtcNow
        };

        if (_isMongoDbActive && _enrollCollection != null)
        {
            try
            {
                await _enrollCollection.InsertOneAsync(enrollment);
                _logger.LogInformation("Enrolled course {Code} into registration {RegId} in MongoDB.", course.Code, registration.Id);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new InvalidOperationException($"Student is already enrolled in course '{course.Code}' for this term registration.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB write failed for course enrollment. Storing in in-memory fallback.");
            }
        }

        _inMemoryEnrollments[enrollment.Id] = enrollment;
        return enrollment;
    }

    public async Task<bool> DropCourseEnrollmentAsync(string registrationId, string courseId)
    {
        var registration = await GetRegistrationByIdAsync(registrationId);
        if (registration == null) return false;

        if (registration.Status.Equals("WITHDRAWN", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot modify course enrollments in a withdrawn term registration.");
        }

        if (_isMongoDbActive && _enrollCollection != null)
        {
            try
            {
                var res = await _enrollCollection.DeleteOneAsync(e => e.RegistrationId == registrationId && e.CourseId == courseId);
                var key = _inMemoryEnrollments.Values.FirstOrDefault(e => e.RegistrationId == registrationId && e.CourseId == courseId)?.Id;
                if (key != null) _inMemoryEnrollments.TryRemove(key, out _);
                return res.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB delete failed for course enrollment {RegId} {CourseId}.", registrationId, courseId);
            }
        }

        var match = _inMemoryEnrollments.Values.FirstOrDefault(e => e.RegistrationId == registrationId && e.CourseId == courseId);
        if (match != null)
        {
            return _inMemoryEnrollments.TryRemove(match.Id, out _);
        }

        return false;
    }

    public async Task<List<CourseEnrollment>> GetEnrollmentsByCourseAsync(string courseId, string? academicPeriodCode = null)
    {
        if (_isMongoDbActive && _enrollCollection != null)
        {
            try
            {
                var builder = Builders<CourseEnrollment>.Filter;
                var filter = (builder.Eq(e => e.CourseId, courseId) | builder.Eq(e => e.CourseCode, courseId))
                             & builder.Eq(e => e.Status, "ENROLLED");

                if (!string.IsNullOrWhiteSpace(academicPeriodCode) && !academicPeriodCode.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    filter &= builder.Eq(e => e.AcademicPeriodCode, academicPeriodCode);
                }

                var list = await _enrollCollection.Find(filter).ToListAsync();
                if (list != null && list.Count > 0)
                {
                    return list;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB lookup failed in GetEnrollmentsByCourseAsync.");
            }
        }

        var q = _inMemoryEnrollments.Values
            .Where(e => (e.CourseId == courseId || e.CourseCode.Equals(courseId, StringComparison.OrdinalIgnoreCase))
                        && e.Status.Equals("ENROLLED", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(academicPeriodCode) && !academicPeriodCode.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(e => e.AcademicPeriodCode.Equals(academicPeriodCode, StringComparison.OrdinalIgnoreCase));
        }

        return q.ToList();
    }
}
