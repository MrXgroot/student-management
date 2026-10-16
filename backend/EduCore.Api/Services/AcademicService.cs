using System.Collections.Concurrent;
using EduCore.Api.Data;
using EduCore.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EduCore.Api.Services;

public class AcademicService : IAcademicService
{
    private readonly ILogger<AcademicService> _logger;
    private readonly IMongoCollection<ProgramInfo>? _programsCollection;
    private readonly IMongoCollection<AcademicPeriod>? _periodsCollection;
    private readonly IMongoCollection<Course>? _coursesCollection;

    private readonly ConcurrentDictionary<string, ProgramInfo> _inMemoryPrograms = new();
    private readonly ConcurrentDictionary<string, AcademicPeriod> _inMemoryPeriods = new();
    private readonly ConcurrentDictionary<string, Course> _inMemoryCourses = new();
    private readonly bool _allowInMemoryFallback;
    private bool _isMongoDbActive;

    public bool IsMongoDbActive => _isMongoDbActive;

    [ActivatorUtilitiesConstructor]
    public AcademicService(IMongoDbContext dbContext, ILogger<AcademicService> logger)
    {
        _logger = logger;
        _isMongoDbActive = dbContext.IsAvailable;
        _allowInMemoryFallback = dbContext.AllowInMemoryFallback;

        if (_isMongoDbActive && dbContext.Database != null)
        {
            try
            {
                _programsCollection = dbContext.GetCollection<ProgramInfo>("programs");
                _periodsCollection = dbContext.GetCollection<AcademicPeriod>("academicPeriods");
                _coursesCollection = dbContext.GetCollection<Course>("courses");

                // Create indexes
                var progIndex = Builders<ProgramInfo>.IndexKeys.Ascending(p => p.Code);
                _programsCollection.Indexes.CreateOne(new CreateIndexModel<ProgramInfo>(progIndex, new CreateIndexOptions { Unique = true }));

                var courseCodeIndex = Builders<Course>.IndexKeys.Ascending(c => c.Code);
                _coursesCollection.Indexes.CreateOne(new CreateIndexModel<Course>(courseCodeIndex, new CreateIndexOptions { Unique = true }));

                var courseProgSemIndex = Builders<Course>.IndexKeys.Ascending(c => c.ProgramCode).Ascending(c => c.Semester);
                _coursesCollection.Indexes.CreateOne(new CreateIndexModel<Course>(courseProgSemIndex));
            }
            catch (Exception ex)
            {
                _isMongoDbActive = false;
                _programsCollection = null;
                _periodsCollection = null;
                _coursesCollection = null;
                _logger.LogWarning(ex, "Error initializing Academic collections/indexes in MongoDB. Using resilient in-memory storage.");
            }
        }

        InitializeSeeds();
    }



    private void InitializeSeeds()
    {
        var seedPrograms = GetSeedPrograms();
        var seedPeriods = GetSeedPeriods();
        var seedCourses = GetSeedCourses();

        if (_isMongoDbActive && _programsCollection != null && _periodsCollection != null && _coursesCollection != null)
        {
            try
            {
                if (_programsCollection.CountDocuments(FilterDefinition<ProgramInfo>.Empty) == 0)
                {
                    _programsCollection.InsertMany(seedPrograms);
                    _logger.LogInformation("Seeded {Count} academic programs into MongoDB.", seedPrograms.Count);
                }

                if (_periodsCollection.CountDocuments(FilterDefinition<AcademicPeriod>.Empty) == 0)
                {
                    _periodsCollection.InsertMany(seedPeriods);
                    _logger.LogInformation("Seeded {Count} academic periods into MongoDB.", seedPeriods.Count);
                }
                else
                {
                    var has2026 = _periodsCollection.Find(p => p.TermCode == "2026-ODD").Any();
                    if (!has2026)
                    {
                        _periodsCollection.DeleteMany(p => p.AcademicYear == "2024-2025");
                        _periodsCollection.InsertMany(seedPeriods);
                        _logger.LogInformation("Updated stale 2024-2025 periods to current 2026-2027 academic periods.");
                    }
                }

                if (_coursesCollection.CountDocuments(FilterDefinition<Course>.Empty) == 0)
                {
                    _coursesCollection.InsertMany(seedCourses);
                    _logger.LogInformation("Seeded {Count} academic courses into MongoDB.", seedCourses.Count);
                }

                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to seed Academic data into MongoDB. Falling back to in-memory.");
                _isMongoDbActive = false;
            }
        }

        if (!_allowInMemoryFallback)
        {
            _logger.LogWarning("MongoDB is not active and in-memory fallback is disabled in this environment. In-memory academic seeds will not be initialized.");
            return;
        }

        _inMemoryPrograms.Clear();
        foreach (var p in seedPrograms) _inMemoryPrograms[p.Id] = p;

        _inMemoryPeriods.Clear();
        foreach (var period in seedPeriods) _inMemoryPeriods[period.Id] = period;

        _inMemoryCourses.Clear();
        foreach (var c in seedCourses) _inMemoryCourses[c.Id] = c;
    }

    private void EnsurePersistenceAvailable()
    {
        if (!_isMongoDbActive && !_allowInMemoryFallback)
        {
            throw new InvalidOperationException("MongoDB persistence is unavailable and in-memory fallback is disabled in this environment.");
        }
    }

    public async Task<List<ProgramInfo>> GetProgramsAsync()
    {
        EnsurePersistenceAvailable();

        if (_isMongoDbActive && _programsCollection != null)
        {
            try
            {
                return await _programsCollection.Find(FilterDefinition<ProgramInfo>.Empty).ToListAsync();
            }
            catch (Exception ex)
            {
                if (!_allowInMemoryFallback)
                {
                    _logger.LogError(ex, "MongoDB read failed for programs and in-memory fallback is disabled in Production.");
                    throw;
                }
                _logger.LogWarning(ex, "MongoDB read failed for programs. Using in-memory fallback.");
            }
        }

        return _inMemoryPrograms.Values.OrderBy(p => p.Name).ToList();
    }

    public async Task<ProgramDetailDto?> GetProgramDetailsAsync(string code)
    {
        var programs = await GetProgramsAsync();
        var program = programs.FirstOrDefault(p => p.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (program == null) return null;

        var allCourses = await GetCoursesAsync(null, program.Code, null, "ALL");

        return new ProgramDetailDto
        {
            Program = program,
            CourseCount = allCourses.Count,
            TotalCredits = allCourses.Sum(c => c.Credits),
            Courses = allCourses.OrderBy(c => c.Semester).ThenBy(c => c.Code).ToList()
        };
    }

    public async Task<List<AcademicPeriod>> GetAcademicPeriodsAsync()
    {
        EnsurePersistenceAvailable();

        if (_isMongoDbActive && _periodsCollection != null)
        {
            try
            {
                return await _periodsCollection.Find(FilterDefinition<AcademicPeriod>.Empty).ToListAsync();
            }
            catch (Exception ex)
            {
                if (!_allowInMemoryFallback)
                {
                    _logger.LogError(ex, "MongoDB read failed for academic periods and in-memory fallback is disabled in Production.");
                    throw;
                }
                _logger.LogWarning(ex, "MongoDB read failed for academic periods. Using in-memory fallback.");
            }
        }

        return _inMemoryPeriods.Values.OrderByDescending(p => p.StartDate).ToList();
    }

    public async Task<AcademicPeriod?> GetCurrentAcademicPeriodAsync()
    {
        var periods = await GetAcademicPeriodsAsync();
        return periods.FirstOrDefault(p => p.IsCurrent) ?? periods.FirstOrDefault();
    }

    public async Task<List<Course>> GetCoursesAsync(string? search, string? program, int? semester, string? status)
    {
        EnsurePersistenceAvailable();

        if (_isMongoDbActive && _coursesCollection != null)
        {
            try
            {
                var builder = Builders<Course>.Filter;
                var filter = builder.Empty;

                if (!string.IsNullOrWhiteSpace(program) && !program.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    filter &= builder.Eq(c => c.ProgramCode, program);
                }

                if (semester.HasValue && semester.Value > 0)
                {
                    filter &= builder.Eq(c => c.Semester, semester.Value);
                }

                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    filter &= builder.Eq(c => c.Status, status.ToUpperInvariant());
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim();
                    var regex = new BsonRegularExpression(s, "i");
                    var searchFilter = builder.Regex(c => c.Code, regex) |
                                       builder.Regex(c => c.Title, regex) |
                                       builder.Regex(c => c.Department, regex);
                    filter &= searchFilter;
                }

                return await _coursesCollection.Find(filter)
                    .SortBy(c => c.ProgramCode)
                    .ThenBy(c => c.Semester)
                    .ThenBy(c => c.Code)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                if (!_allowInMemoryFallback)
                {
                    _logger.LogError(ex, "MongoDB read failed for courses and in-memory fallback is disabled in Production.");
                    throw;
                }
                _logger.LogWarning(ex, "MongoDB read failed for courses. Using in-memory fallback.");
            }
        }

        var query = _inMemoryCourses.Values.AsQueryable();

        if (!string.IsNullOrWhiteSpace(program) && !program.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(c => c.ProgramCode.Equals(program, StringComparison.OrdinalIgnoreCase));
        }

        if (semester.HasValue && semester.Value > 0)
        {
            query = query.Where(c => c.Semester == semester.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(c => c.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(c => c.Code.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                                     c.Title.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                                     c.Department.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderBy(c => c.ProgramCode)
                    .ThenBy(c => c.Semester)
                    .ThenBy(c => c.Code)
                    .ToList();
    }

    public async Task<Course?> GetCourseByIdAsync(string id)
    {
        if (_isMongoDbActive && _coursesCollection != null)
        {
            try
            {
                var course = await _coursesCollection.Find(c => c.Id == id).FirstOrDefaultAsync();
                if (course != null) return course;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB read failed for course {Id}.", id);
            }
        }

        _inMemoryCourses.TryGetValue(id, out var fallback);
        return fallback;
    }

    public async Task<Course?> GetCourseByCodeAsync(string code)
    {
        var cleanCode = code.Trim().ToUpperInvariant();
        if (_isMongoDbActive && _coursesCollection != null)
        {
            try
            {
                var course = await _coursesCollection.Find(c => c.Code == cleanCode).FirstOrDefaultAsync();
                if (course != null) return course;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB read failed for course code {Code}.", cleanCode);
            }
        }

        return _inMemoryCourses.Values.FirstOrDefault(c => c.Code.Equals(cleanCode, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Course> CreateCourseAsync(CreateCourseDto dto)
    {
        var cleanCode = dto.Code.Trim().ToUpperInvariant();

        // Check for duplicate code
        var existing = await GetCourseByCodeAsync(cleanCode);
        if (existing != null)
        {
            throw new InvalidOperationException($"A course with code '{cleanCode}' already exists ({existing.Title}).");
        }

        var course = new Course
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Code = cleanCode,
            Title = dto.Title.Trim(),
            Credits = dto.Credits,
            ProgramCode = dto.ProgramCode.Trim(),
            Semester = dto.Semester,
            Department = dto.Department.Trim(),
            CourseType = string.IsNullOrWhiteSpace(dto.CourseType) ? "CORE" : dto.CourseType.Trim().ToUpperInvariant(),
            Description = dto.Description.Trim(),
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (_isMongoDbActive && _coursesCollection != null)
        {
            try
            {
                await _coursesCollection.InsertOneAsync(course);
                _logger.LogInformation("Created course {Code} ({Title}) in MongoDB.", course.Code, course.Title);
                _inMemoryCourses[course.Id] = course;
                return course;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB write failed for course {Code}. Storing in memory fallback.", course.Code);
            }
        }

        _inMemoryCourses[course.Id] = course;
        return course;
    }

    public async Task<Course?> UpdateCourseAsync(string id, UpdateCourseDto dto)
    {
        var existing = await GetCourseByIdAsync(id);
        if (existing == null) return null;

        var cleanCode = dto.Code.Trim().ToUpperInvariant();

        // If code changed, check uniqueness
        if (!existing.Code.Equals(cleanCode, StringComparison.OrdinalIgnoreCase))
        {
            var conflict = await GetCourseByCodeAsync(cleanCode);
            if (conflict != null && conflict.Id != id)
            {
                throw new InvalidOperationException($"Cannot change code to '{cleanCode}' as it already belongs to '{conflict.Title}'.");
            }
        }

        existing.Code = cleanCode;
        existing.Title = dto.Title.Trim();
        existing.Credits = dto.Credits;
        existing.ProgramCode = dto.ProgramCode.Trim();
        existing.Semester = dto.Semester;
        existing.Department = dto.Department.Trim();
        existing.CourseType = dto.CourseType?.Trim().ToUpperInvariant() ?? existing.CourseType;
        existing.Description = dto.Description.Trim();
        existing.Status = dto.Status?.Trim().ToUpperInvariant() ?? existing.Status;
        existing.UpdatedAt = DateTime.UtcNow;

        if (_isMongoDbActive && _coursesCollection != null)
        {
            try
            {
                var filter = Builders<Course>.Filter.Eq(c => c.Id, id);
                await _coursesCollection.ReplaceOneAsync(filter, existing);
                _inMemoryCourses[id] = existing;
                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB update failed for course {Id}.", id);
            }
        }

        _inMemoryCourses[id] = existing;
        return existing;
    }

    public async Task<bool> DeleteCourseAsync(string id)
    {
        if (_isMongoDbActive && _coursesCollection != null)
        {
            try
            {
                var res = await _coursesCollection.DeleteOneAsync(c => c.Id == id);
                _inMemoryCourses.TryRemove(id, out _);
                return res.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB delete failed for course {Id}.", id);
            }
        }

        return _inMemoryCourses.TryRemove(id, out _);
    }

    public async Task ResetAcademicDemoDataAsync()
    {
        var seedPrograms = GetSeedPrograms();
        var seedPeriods = GetSeedPeriods();
        var seedCourses = GetSeedCourses();

        if (_isMongoDbActive && _programsCollection != null && _periodsCollection != null && _coursesCollection != null)
        {
            try
            {
                await _programsCollection.DeleteManyAsync(FilterDefinition<ProgramInfo>.Empty);
                await _programsCollection.InsertManyAsync(seedPrograms);

                await _periodsCollection.DeleteManyAsync(FilterDefinition<AcademicPeriod>.Empty);
                await _periodsCollection.InsertManyAsync(seedPeriods);

                await _coursesCollection.DeleteManyAsync(FilterDefinition<Course>.Empty);
                await _coursesCollection.InsertManyAsync(seedCourses);
                _logger.LogInformation("Reset academic programs, periods, and courses in MongoDB.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reset MongoDB academic data.");
            }
        }

        _inMemoryPrograms.Clear();
        foreach (var p in seedPrograms) _inMemoryPrograms[p.Id] = p;

        _inMemoryPeriods.Clear();
        foreach (var period in seedPeriods) _inMemoryPeriods[period.Id] = period;

        _inMemoryCourses.Clear();
        foreach (var c in seedCourses) _inMemoryCourses[c.Id] = c;
    }

    private static List<ProgramInfo> GetSeedPrograms() => new()
    {
        new ProgramInfo
        {
            Id = "670a00000000000000000001",
            Code = "BCA",
            Name = "Bachelor of Computer Applications",
            Award = "Undergraduate",
            Duration = "3 Years (6 Semesters)",
            DurationYears = 3,
            TotalSemesters = 6,
            Department = "Computer Applications",
            CurriculumSummary = "C Programming, Data Structures, Web Technologies, Database Systems, Java, Operating Systems, Cloud Computing",
            Color = "#0d9488",
            Status = "ACTIVE",
            TotalCreditsRequired = 132
        },
        new ProgramInfo
        {
            Id = "670a00000000000000000002",
            Code = "B.Sc. Computer Science",
            Name = "Bachelor of Science in Computer Science",
            Award = "Undergraduate",
            Duration = "3 Years (6 Semesters)",
            DurationYears = 3,
            TotalSemesters = 6,
            Department = "Computer Science",
            CurriculumSummary = "Discrete Mathematics, Python Programming, Computer Architecture, OS, Artificial Intelligence, Algorithms",
            Color = "#0284c7",
            Status = "ACTIVE",
            TotalCreditsRequired = 136
        },
        new ProgramInfo
        {
            Id = "670a00000000000000000003",
            Code = "B.Com.",
            Name = "Bachelor of Commerce",
            Award = "Undergraduate",
            Duration = "3 Years (6 Semesters)",
            DurationYears = 3,
            TotalSemesters = 6,
            Department = "Commerce & Accountancy",
            CurriculumSummary = "Financial Accounting, Corporate Law, Business Economics, Taxation & GST, Cost Accounting, Auditing",
            Color = "#059669",
            Status = "ACTIVE",
            TotalCreditsRequired = 128
        },
        new ProgramInfo
        {
            Id = "670a00000000000000000004",
            Code = "BBA",
            Name = "Bachelor of Business Administration",
            Award = "Undergraduate",
            Duration = "3 Years (6 Semesters)",
            DurationYears = 3,
            TotalSemesters = 6,
            Department = "Management Studies",
            CurriculumSummary = "Principles of Management, Marketing, Organizational Behavior, Business Analytics, Human Resources, Finance",
            Color = "#d97706",
            Status = "ACTIVE",
            TotalCreditsRequired = 130
        },
        new ProgramInfo
        {
            Id = "670a00000000000000000005",
            Code = "MCA",
            Name = "Master of Computer Applications",
            Award = "Postgraduate",
            Duration = "2 Years (4 Semesters)",
            DurationYears = 2,
            TotalSemesters = 4,
            Department = "Computer Applications",
            CurriculumSummary = "Advanced Algorithms, Cloud Computing, Distributed Systems, Full Stack Web Dev, Machine Learning, DevOps",
            Color = "#9333ea",
            Status = "ACTIVE",
            TotalCreditsRequired = 92
        },
        new ProgramInfo
        {
            Id = "670a00000000000000000006",
            Code = "MBA",
            Name = "Master of Business Administration",
            Award = "Postgraduate",
            Duration = "2 Years (4 Semesters)",
            DurationYears = 2,
            TotalSemesters = 4,
            Department = "Management Studies",
            CurriculumSummary = "Strategic Management, Corporate Finance, Consumer Behavior, Operations & Supply Chain, Business Analytics",
            Color = "#e11d48",
            Status = "ACTIVE",
            TotalCreditsRequired = 96
        }
    };

    private static List<AcademicPeriod> GetSeedPeriods() => new()
    {
        new AcademicPeriod
        {
            Id = "670b00000000000000000001",
            AcademicYear = "2026-2027",
            TermCode = "2026-ODD",
            TermName = "Odd Semester (Semesters 1, 3, 5)",
            StartDate = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc),
            IsCurrent = true,
            Status = "ACTIVE"
        },
        new AcademicPeriod
        {
            Id = "670b00000000000000000002",
            AcademicYear = "2026-2027",
            TermCode = "2027-EVEN",
            TermName = "Even Semester (Semesters 2, 4, 6)",
            StartDate = new DateTime(2027, 1, 5, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2027, 5, 30, 0, 0, 0, DateTimeKind.Utc),
            IsCurrent = false,
            Status = "UPCOMING"
        }
    };

    private static List<Course> GetSeedCourses() => new()
    {
        // BCA Courses
        new Course
        {
            Id = "670c00000000000000000001",
            Code = "BCA-101",
            Title = "Programming in C",
            Credits = 4,
            ProgramCode = "BCA",
            Semester = 1,
            Department = "Computer Applications",
            CourseType = "CORE",
            Description = "Fundamentals of programming using ANSI C, memory management, pointers, and structures.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000002",
            Code = "BCA-102",
            Title = "Digital Electronics & Computer Organization",
            Credits = 4,
            ProgramCode = "BCA",
            Semester = 1,
            Department = "Computer Applications",
            CourseType = "CORE",
            Description = "Logic gates, combinational logic, sequential circuits, CPU architecture, and bus structures.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000003",
            Code = "BCA-103L",
            Title = "C Programming Laboratory",
            Credits = 2,
            ProgramCode = "BCA",
            Semester = 1,
            Department = "Computer Applications",
            CourseType = "LAB",
            Description = "Hands-on implementation of algorithms, pointers, dynamic memory allocation, and file I/O in C.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000004",
            Code = "BCA-201",
            Title = "Data Structures using C++",
            Credits = 4,
            ProgramCode = "BCA",
            Semester = 2,
            Department = "Computer Applications",
            CourseType = "CORE",
            Description = "Arrays, stacks, queues, linked lists, trees, graphs, sorting, and asymptotic complexity analysis.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000005",
            Code = "BCA-202",
            Title = "Database Management Systems",
            Credits = 4,
            ProgramCode = "BCA",
            Semester = 2,
            Department = "Computer Applications",
            CourseType = "CORE",
            Description = "Relational algebra, ER modeling, SQL queries, normalization, ACID properties, and indexing.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000006",
            Code = "BCA-301",
            Title = "Object Oriented Programming with Java",
            Credits = 4,
            ProgramCode = "BCA",
            Semester = 3,
            Department = "Computer Applications",
            CourseType = "CORE",
            Description = "Classes, inheritance, polymorphism, abstract classes, collections framework, and multithreading.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000007",
            Code = "BCA-302",
            Title = "Operating Systems & Linux Shell",
            Credits = 4,
            ProgramCode = "BCA",
            Semester = 3,
            Department = "Computer Applications",
            CourseType = "CORE",
            Description = "Process scheduling, synchronization, deadlocks, virtual memory, and Bash scripting.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000008",
            Code = "BCA-401",
            Title = "Web Technologies & Full Stack Dev",
            Credits = 4,
            ProgramCode = "BCA",
            Semester = 4,
            Department = "Computer Applications",
            CourseType = "CORE",
            Description = "HTML5, CSS3, JavaScript ES6+, RESTful APIs, React fundamentals, and Node.js backend integration.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000009",
            Code = "BCA-501",
            Title = "Software Engineering & Agile Methodologies",
            Credits = 3,
            ProgramCode = "BCA",
            Semester = 5,
            Department = "Computer Applications",
            CourseType = "CORE",
            Description = "SDLC models, Scrum framework, requirement engineering, UML design diagrams, and software testing.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000010",
            Code = "BCA-601",
            Title = "Cloud Computing & DevOps",
            Credits = 3,
            ProgramCode = "BCA",
            Semester = 6,
            Department = "Computer Applications",
            CourseType = "ELECTIVE",
            Description = "Cloud virtualization, AWS/Azure fundamentals, Docker containerization, CI/CD pipelines, and microservices.",
            Status = "ACTIVE"
        },

        // B.Sc. Computer Science Courses
        new Course
        {
            Id = "670c00000000000000000011",
            Code = "BSC-101",
            Title = "Discrete Mathematical Structures",
            Credits = 4,
            ProgramCode = "B.Sc. Computer Science",
            Semester = 1,
            Department = "Computer Science",
            CourseType = "CORE",
            Description = "Set theory, propositional logic, relations, recurrence relations, and graph theory.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000012",
            Code = "BSC-102",
            Title = "Python Programming & Problem Solving",
            Credits = 4,
            ProgramCode = "B.Sc. Computer Science",
            Semester = 1,
            Department = "Computer Science",
            CourseType = "CORE",
            Description = "Python syntax, functional programming, OOP in Python, NumPy, and scientific data manipulation.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000013",
            Code = "BSC-201",
            Title = "Computer System Architecture",
            Credits = 4,
            ProgramCode = "B.Sc. Computer Science",
            Semester = 2,
            Department = "Computer Science",
            CourseType = "CORE",
            Description = "Instruction set architecture, pipelining, RISC vs CISC, memory hierarchy, and cache coherence.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000014",
            Code = "BSC-202",
            Title = "Design & Analysis of Algorithms",
            Credits = 4,
            ProgramCode = "B.Sc. Computer Science",
            Semester = 2,
            Department = "Computer Science",
            CourseType = "CORE",
            Description = "Divide and conquer, greedy strategies, dynamic programming, backtracking, and NP-completeness.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000015",
            Code = "BSC-301",
            Title = "Artificial Intelligence & Knowledge Systems",
            Credits = 4,
            ProgramCode = "B.Sc. Computer Science",
            Semester = 3,
            Department = "Computer Science",
            CourseType = "CORE",
            Description = "Search algorithms (A*, minimax), constraint satisfaction, logic inference, and heuristic search.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000016",
            Code = "BSC-401",
            Title = "Computer Networks & Protocol Design",
            Credits = 4,
            ProgramCode = "B.Sc. Computer Science",
            Semester = 4,
            Department = "Computer Science",
            CourseType = "CORE",
            Description = "OSI reference model, TCP/IP suite, routing protocols, flow control, and network security basics.",
            Status = "ACTIVE"
        },

        // B.Com. Courses
        new Course
        {
            Id = "670c00000000000000000017",
            Code = "BCM-101",
            Title = "Financial Accounting",
            Credits = 4,
            ProgramCode = "B.Com.",
            Semester = 1,
            Department = "Commerce & Accountancy",
            CourseType = "CORE",
            Description = "Accounting concepts, journal entries, ledger posting, trial balance, and preparation of final accounts.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000018",
            Code = "BCM-102",
            Title = "Business Economics & Decision Making",
            Credits = 3,
            ProgramCode = "B.Com.",
            Semester = 1,
            Department = "Commerce & Accountancy",
            CourseType = "CORE",
            Description = "Demand forecasting, market structures, price elasticity, cost curves, and macroeconomic indicators.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000019",
            Code = "BCM-201",
            Title = "Corporate Law & Governance",
            Credits = 4,
            ProgramCode = "B.Com.",
            Semester = 2,
            Department = "Commerce & Accountancy",
            CourseType = "CORE",
            Description = "Indian Companies Act 2013, incorporation, prospectus, director liabilities, and shareholder rights.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000020",
            Code = "BCM-301",
            Title = "Direct Taxation & GST Regulatory Framework",
            Credits = 4,
            ProgramCode = "B.Com.",
            Semester = 3,
            Department = "Commerce & Accountancy",
            CourseType = "CORE",
            Description = "Income tax provisions, heads of income, deductions under chapter VI-A, and GST compliance in India.",
            Status = "ACTIVE"
        },

        // BBA Courses
        new Course
        {
            Id = "670c00000000000000000021",
            Code = "BBA-101",
            Title = "Principles & Practice of Management",
            Credits = 4,
            ProgramCode = "BBA",
            Semester = 1,
            Department = "Management Studies",
            CourseType = "CORE",
            Description = "Planning, organizing, staffing, directing, controlling, and contemporary leadership frameworks.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000022",
            Code = "BBA-102",
            Title = "Business Communication & Professional Ethics",
            Credits = 3,
            ProgramCode = "BBA",
            Semester = 1,
            Department = "Management Studies",
            CourseType = "CORE",
            Description = "Executive report writing, business presentations, corporate etiquette, and cross-cultural communication.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000023",
            Code = "BBA-201",
            Title = "Organizational Behavior & Human Dynamics",
            Credits = 4,
            ProgramCode = "BBA",
            Semester = 2,
            Department = "Management Studies",
            CourseType = "CORE",
            Description = "Personality, motivation theories, group dynamics, organizational culture, and conflict resolution.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000024",
            Code = "BBA-202",
            Title = "Marketing Management",
            Credits = 4,
            ProgramCode = "BBA",
            Semester = 2,
            Department = "Management Studies",
            CourseType = "CORE",
            Description = "Market segmentation, targeting, positioning (STP), 4Ps of marketing, and consumer behavior analysis.",
            Status = "ACTIVE"
        },

        // MCA Courses
        new Course
        {
            Id = "670c00000000000000000025",
            Code = "MCA-101",
            Title = "Advanced Data Structures & Algorithms",
            Credits = 4,
            ProgramCode = "MCA",
            Semester = 1,
            Department = "Computer Applications",
            CourseType = "CORE",
            Description = "Advanced tree structures, B-trees, Fibonacci heaps, network flow, randomized algorithms, and complexity.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000026",
            Code = "MCA-102",
            Title = "Enterprise Java & Cloud Microservices",
            Credits = 4,
            ProgramCode = "MCA",
            Semester = 1,
            Department = "Computer Applications",
            CourseType = "CORE",
            Description = "Spring Boot, JPA/Hibernate, Kafka messaging, API gateway, Docker containers, and Kubernetes orchestration.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000027",
            Code = "MCA-201",
            Title = "Machine Learning & Predictive Modeling",
            Credits = 4,
            ProgramCode = "MCA",
            Semester = 2,
            Department = "Computer Applications",
            CourseType = "CORE",
            Description = "Supervised/unsupervised learning, neural networks, decision trees, model evaluation, and scikit-learn.",
            Status = "ACTIVE"
        },

        // MBA Courses
        new Course
        {
            Id = "670c00000000000000000028",
            Code = "MBA-101",
            Title = "Strategic Management & Competitive Advantage",
            Credits = 4,
            ProgramCode = "MBA",
            Semester = 1,
            Department = "Management Studies",
            CourseType = "CORE",
            Description = "Porter's Five Forces, VRIO analysis, strategic formulation, corporate mergers, and competitive positioning.",
            Status = "ACTIVE"
        },
        new Course
        {
            Id = "670c00000000000000000029",
            Code = "MBA-201",
            Title = "Corporate Finance & Valuation",
            Credits = 4,
            ProgramCode = "MBA",
            Semester = 2,
            Department = "Management Studies",
            CourseType = "CORE",
            Description = "DCF valuation, capital budgeting, cost of capital (WACC), dividend policy, and financial risk hedging.",
            Status = "ACTIVE"
        }
    };
}
