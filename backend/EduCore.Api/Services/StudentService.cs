using System.Collections.Concurrent;
using EduCore.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EduCore.Api.Services;

public class StudentService : IStudentService
{
    private readonly ILogger<StudentService> _logger;
    private readonly IMongoCollection<Student>? _collection;
    private readonly ConcurrentDictionary<string, Student> _inMemoryStore = new();
    private bool _isMongoDbActive;
    private int _sequenceCounter = 1000;

    public bool IsMongoDbActive => _isMongoDbActive;

    public StudentService(IConfiguration config, ILogger<StudentService> logger)
    {
        _logger = logger;
        var connectionString = config.GetConnectionString("MongoDb") ?? "mongodb://localhost:27017";
        var databaseName = config["DatabaseName"] ?? "educore_mvp";

        try
        {
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);
            var client = new MongoClient(settings);
            var database = client.GetDatabase(databaseName);
            _collection = database.GetCollection<Student>("students");

            // Test connectivity with a ping
            database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(2500);
            _isMongoDbActive = true;
            _logger.LogInformation("Connected successfully to MongoDB at {ConnectionString}, Database: {Db}", connectionString, databaseName);

            // Ensure unique index on admissionNumber
            var indexKeys = Builders<Student>.IndexKeys.Ascending(s => s.AdmissionNumber);
            var indexOptions = new CreateIndexOptions { Unique = true, Sparse = false };
            _collection.Indexes.CreateOne(new CreateIndexModel<Student>(indexKeys, indexOptions));
        }
        catch (Exception ex)
        {
            _isMongoDbActive = false;
            _collection = null;
            _logger.LogWarning(ex, "MongoDB is unavailable. Defaulting to In-Memory resilient fallback storage.");
        }

        InitializeDemoData();
    }

    private void InitializeDemoData()
    {
        var seeds = GetInitialSeedStudents();

        if (_isMongoDbActive && _collection != null)
        {
            try
            {
                var count = _collection.CountDocuments(FilterDefinition<Student>.Empty);
                if (count == 0)
                {
                    _collection.InsertMany(seeds);
                    _logger.LogInformation("Seeded {Count} fictional Indian student records into MongoDB.", seeds.Count);
                }
                else
                {
                    _logger.LogInformation("MongoDB already contains {Count} student records. Preserving existing database state.", count);
                }
                _sequenceCounter = (int)(count + seeds.Count + 100);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking/seeding MongoDB. Falling back to in-memory store.");
                _isMongoDbActive = false;
            }
        }

        // Fallback or mirror in memory
        _inMemoryStore.Clear();
        foreach (var s in seeds)
        {
            _inMemoryStore[s.Id] = s;
        }
        _sequenceCounter = seeds.Count + 100;
        _logger.LogInformation("Initialized {Count} demo student records in In-Memory store.", seeds.Count);
    }

    public async Task<List<Student>> GetStudentsAsync(string? search, string? status, string? program)
    {
        if (_isMongoDbActive && _collection != null)
        {
            try
            {
                var builder = Builders<Student>.Filter;
                var filter = builder.Empty;

                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    filter &= builder.Eq(s => s.LifecycleStatus, status.ToUpperInvariant());
                }

                if (!string.IsNullOrWhiteSpace(program) && !program.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    filter &= builder.Eq(s => s.Program, program);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim();
                    var searchFilter = builder.Regex(x => x.FirstName, new BsonRegularExpression(s, "i"))
                        | builder.Regex(x => x.LastName, new BsonRegularExpression(s, "i"))
                        | builder.Regex(x => x.AdmissionNumber, new BsonRegularExpression(s, "i"))
                        | builder.Regex(x => x.RollNumber, new BsonRegularExpression(s, "i"))
                        | builder.Regex(x => x.Email, new BsonRegularExpression(s, "i"));
                    filter &= searchFilter;
                }

                return await _collection.Find(filter)
                    .SortByDescending(s => s.AdmissionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB read query failed. Reading from in-memory fallback.");
            }
        }

        // In-Memory Query
        var query = _inMemoryStore.Values.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.LifecycleStatus.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(program) && !program.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.Program.Equals(program, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.FirstName.ToLowerInvariant().Contains(s) ||
                x.LastName.ToLowerInvariant().Contains(s) ||
                x.AdmissionNumber.ToLowerInvariant().Contains(s) ||
                (x.RollNumber != null && x.RollNumber.ToLowerInvariant().Contains(s)) ||
                x.Email.ToLowerInvariant().Contains(s));
        }

        return query.OrderByDescending(s => s.AdmissionDate).ToList();
    }

    public async Task<Student?> GetStudentByIdAsync(string id)
    {
        if (_isMongoDbActive && _collection != null)
        {
            try
            {
                var doc = await _collection.Find(s => s.Id == id).FirstOrDefaultAsync();
                if (doc != null) return doc;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB lookup for {Id} failed.", id);
            }
        }

        _inMemoryStore.TryGetValue(id, out var student);
        return student;
    }

    public async Task<Student> CreateStudentAsync(CreateStudentDto dto)
    {
        var seq = Interlocked.Increment(ref _sequenceCounter);
        var currentYear = DateTime.UtcNow.Year;
        var admissionNumber = $"ADM-{currentYear}-{seq:D5}";

        var student = new Student
        {
            Id = ObjectId.GenerateNewId().ToString(),
            AdmissionNumber = admissionNumber,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Phone = dto.Phone.Trim(),
            DateOfBirth = dto.DateOfBirth ?? new DateTime(2004, 5, 15),
            Gender = dto.Gender ?? "Other",
            Program = dto.Program ?? "BCA",
            CurrentSemester = dto.CurrentSemester,
            LifecycleStatus = "ADMITTED",
            Address = dto.Address ?? "Bengaluru, Karnataka",
            GuardianName = dto.GuardianName ?? string.Empty,
            GuardianPhone = dto.GuardianPhone ?? string.Empty,
            AdmissionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (_isMongoDbActive && _collection != null)
        {
            try
            {
                await _collection.InsertOneAsync(student);
                _logger.LogInformation("Created student {AdmissionNumber} in MongoDB.", student.AdmissionNumber);
                _inMemoryStore[student.Id] = student;
                return student;
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                // Unique admission number retry
                var retrySeq = Random.Shared.Next(30000, 99999);
                student.AdmissionNumber = $"ADM-{currentYear}-{retrySeq:D5}";
                await _collection.InsertOneAsync(student);
                _inMemoryStore[student.Id] = student;
                return student;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB write failed. Storing student in in-memory fallback.");
            }
        }

        _inMemoryStore[student.Id] = student;
        return student;
    }

    public async Task<Student?> UpdateStudentAsync(string id, UpdateStudentDto dto)
    {
        var existing = await GetStudentByIdAsync(id);
        if (existing == null) return null;

        existing.FirstName = dto.FirstName.Trim();
        existing.LastName = dto.LastName.Trim();
        existing.Email = dto.Email.Trim().ToLowerInvariant();
        existing.Phone = dto.Phone.Trim();
        if (dto.DateOfBirth.HasValue) existing.DateOfBirth = dto.DateOfBirth.Value;
        existing.Gender = dto.Gender ?? existing.Gender;
        existing.Program = dto.Program ?? existing.Program;
        existing.CurrentSemester = dto.CurrentSemester;
        existing.LifecycleStatus = dto.LifecycleStatus ?? existing.LifecycleStatus;
        existing.RollNumber = dto.RollNumber?.Trim();
        existing.Address = dto.Address ?? existing.Address;
        existing.GuardianName = dto.GuardianName ?? existing.GuardianName;
        existing.GuardianPhone = dto.GuardianPhone ?? existing.GuardianPhone;
        existing.UpdatedAt = DateTime.UtcNow;

        if (_isMongoDbActive && _collection != null)
        {
            try
            {
                var filter = Builders<Student>.Filter.Eq(s => s.Id, id);
                await _collection.ReplaceOneAsync(filter, existing);
                _inMemoryStore[id] = existing;
                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB update failed for {Id}.", id);
            }
        }

        _inMemoryStore[id] = existing;
        return existing;
    }

    public async Task<bool> DeleteStudentAsync(string id)
    {
        if (_isMongoDbActive && _collection != null)
        {
            try
            {
                var result = await _collection.DeleteOneAsync(s => s.Id == id);
                _inMemoryStore.TryRemove(id, out _);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB delete failed for {Id}.", id);
            }
        }

        return _inMemoryStore.TryRemove(id, out _);
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var allStudents = await GetStudentsAsync(null, "ALL", "ALL");

        var programs = allStudents
            .GroupBy(s => s.Program)
            .Select(g => new ProgramStatDto
            {
                Program = g.Key,
                Count = g.Count(),
                ActiveCount = g.Count(s => s.LifecycleStatus == "ACTIVE")
            })
            .OrderByDescending(p => p.Count)
            .ToList();

        var recent = allStudents
            .OrderByDescending(s => s.AdmissionDate)
            .Take(5)
            .ToList();

        return new DashboardSummaryDto
        {
            TotalStudents = allStudents.Count,
            ActiveStudents = allStudents.Count(s => s.LifecycleStatus == "ACTIVE"),
            AdmittedStudents = allStudents.Count(s => s.LifecycleStatus == "ADMITTED"),
            ProbationStudents = allStudents.Count(s => s.LifecycleStatus == "PROBATION"),
            ProgramCount = programs.Count,
            IsUsingMongoDb = _isMongoDbActive,
            ProgramBreakdown = programs,
            RecentAdmissions = recent
        };
    }

    public async Task ResetDemoDataAsync()
    {
        var seeds = GetInitialSeedStudents();
        if (_isMongoDbActive && _collection != null)
        {
            try
            {
                await _collection.DeleteManyAsync(FilterDefinition<Student>.Empty);
                await _collection.InsertManyAsync(seeds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resetting MongoDB failed. Resetting in-memory store.");
            }
        }

        _inMemoryStore.Clear();
        foreach (var s in seeds)
        {
            _inMemoryStore[s.Id] = s;
        }
    }

    private static List<Student> GetInitialSeedStudents()
    {
        return new List<Student>
        {
            new() {
                Id = "670f00010000000000000001",
                AdmissionNumber = "ADM-2024-00101",
                RollNumber = "24BCA001",
                FirstName = "Ananya",
                LastName = "Rao",
                Email = "ananya.rao@student.vit.edu",
                Phone = "9845012345",
                DateOfBirth = new DateTime(2004, 3, 14),
                Gender = "Female",
                Program = "BCA",
                CurrentSemester = 3,
                LifecycleStatus = "ACTIVE",
                Address = "Jayanagar 4th Block, Bengaluru, Karnataka",
                GuardianName = "Suresh Rao",
                GuardianPhone = "9845098765",
                AdmissionDate = new DateTime(2024, 7, 10, 9, 30, 0, DateTimeKind.Utc)
            },
            new() {
                Id = "670f00010000000000000002",
                AdmissionNumber = "ADM-2026-00102",
                RollNumber = null,
                FirstName = "Rohan",
                LastName = "Shetty",
                Email = "rohan.shetty@student.vit.edu",
                Phone = "9741023456",
                DateOfBirth = new DateTime(2005, 8, 22),
                Gender = "Male",
                Program = "B.Sc. Computer Science",
                CurrentSemester = 1,
                LifecycleStatus = "ADMITTED",
                Address = "Indiranagar 100ft Road, Bengaluru, Karnataka",
                GuardianName = "Manohar Shetty",
                GuardianPhone = "9741087654",
                AdmissionDate = new DateTime(2026, 6, 15, 11, 0, 0, DateTimeKind.Utc)
            },
            new() {
                Id = "670f00010000000000000003",
                AdmissionNumber = "ADM-2023-00045",
                RollNumber = "23BCA012",
                FirstName = "Aditi",
                LastName = "Nair",
                Email = "aditi.nair@student.vit.edu",
                Phone = "9900134567",
                DateOfBirth = new DateTime(2003, 11, 5),
                Gender = "Female",
                Program = "BCA",
                CurrentSemester = 5,
                LifecycleStatus = "ACTIVE",
                Address = "Koramangala 5th Block, Bengaluru, Karnataka",
                GuardianName = "Radhakrishnan Nair",
                GuardianPhone = "9900176543",
                AdmissionDate = new DateTime(2023, 7, 5, 10, 15, 0, DateTimeKind.Utc)
            },
            new() {
                Id = "670f00010000000000000004",
                AdmissionNumber = "ADM-2025-00114",
                RollNumber = "25BBA009",
                FirstName = "Arjun",
                LastName = "Kulkarni",
                Email = "arjun.kulkarni@student.vit.edu",
                Phone = "9880145678",
                DateOfBirth = new DateTime(2005, 1, 19),
                Gender = "Male",
                Program = "BBA",
                CurrentSemester = 2,
                LifecycleStatus = "ACTIVE",
                Address = "Malleshwaram 8th Cross, Bengaluru, Karnataka",
                GuardianName = "Venkatesh Kulkarni",
                GuardianPhone = "9880165432",
                AdmissionDate = new DateTime(2025, 7, 20, 14, 0, 0, DateTimeKind.Utc)
            },
            new() {
                Id = "670f00010000000000000005",
                AdmissionNumber = "ADM-2024-00122",
                RollNumber = "24BCM034",
                FirstName = "Kavya",
                LastName = "Reddy",
                Email = "kavya.reddy@student.vit.edu",
                Phone = "9663156789",
                DateOfBirth = new DateTime(2004, 6, 30),
                Gender = "Female",
                Program = "B.Com.",
                CurrentSemester = 4,
                LifecycleStatus = "PROBATION",
                Address = "HSR Layout Sector 2, Bengaluru, Karnataka",
                GuardianName = "Pratap Reddy",
                GuardianPhone = "9663154321",
                AdmissionDate = new DateTime(2024, 7, 18, 11, 45, 0, DateTimeKind.Utc)
            },
            new() {
                Id = "670f00010000000000000006",
                AdmissionNumber = "ADM-2025-00088",
                RollNumber = "25MCA003",
                FirstName = "Nikhil",
                LastName = "Gowda",
                Email = "nikhil.gowda@student.vit.edu",
                Phone = "9448167890",
                DateOfBirth = new DateTime(2002, 9, 12),
                Gender = "Male",
                Program = "MCA",
                CurrentSemester = 2,
                LifecycleStatus = "ACTIVE",
                Address = "Basavanagudi, Bengaluru, Karnataka",
                GuardianName = "Kempegowda N",
                GuardianPhone = "9448143210",
                AdmissionDate = new DateTime(2025, 8, 2, 10, 0, 0, DateTimeKind.Utc)
            },
            new() {
                Id = "670f00010000000000000007",
                AdmissionNumber = "ADM-2024-00135",
                RollNumber = "24BCS022",
                FirstName = "Meera",
                LastName = "Iyer",
                Email = "meera.iyer@student.vit.edu",
                Phone = "9110178901",
                DateOfBirth = new DateTime(2004, 12, 8),
                Gender = "Female",
                Program = "B.Sc. Computer Science",
                CurrentSemester = 3,
                LifecycleStatus = "ACTIVE",
                Address = "Sadashivanagar, Bengaluru, Karnataka",
                GuardianName = "Subramanian Iyer",
                GuardianPhone = "9110132109",
                AdmissionDate = new DateTime(2024, 7, 14, 15, 30, 0, DateTimeKind.Utc)
            },
            new() {
                Id = "670f00010000000000000008",
                AdmissionNumber = "ADM-2026-00140",
                RollNumber = null,
                FirstName = "Ishaan",
                LastName = "Patil",
                Email = "ishaan.patil@student.vit.edu",
                Phone = "9844189012",
                DateOfBirth = new DateTime(2005, 4, 25),
                Gender = "Male",
                Program = "BCA",
                CurrentSemester = 1,
                LifecycleStatus = "ADMITTED",
                Address = "Rajajinagar 1st Block, Bengaluru, Karnataka",
                GuardianName = "Anand Patil",
                GuardianPhone = "9844121098",
                AdmissionDate = new DateTime(2026, 6, 28, 9, 0, 0, DateTimeKind.Utc)
            },
            new() {
                Id = "670f00010000000000000009",
                AdmissionNumber = "ADM-2024-00067",
                RollNumber = "24MBA018",
                FirstName = "Sneha",
                LastName = "Menon",
                Email = "sneha.menon@student.vit.edu",
                Phone = "9731190123",
                DateOfBirth = new DateTime(2001, 7, 17),
                Gender = "Female",
                Program = "MBA",
                CurrentSemester = 4,
                LifecycleStatus = "ACTIVE",
                Address = "Whitefield Main Road, Bengaluru, Karnataka",
                GuardianName = "Gopinath Menon",
                GuardianPhone = "9731110987",
                AdmissionDate = new DateTime(2024, 8, 10, 12, 30, 0, DateTimeKind.Utc)
            },
            new() {
                Id = "670f00010000000000000010",
                AdmissionNumber = "ADM-2025-00155",
                RollNumber = "25BCM051",
                FirstName = "Rahul",
                LastName = "Deshmukh",
                Email = "rahul.deshmukh@student.vit.edu",
                Phone = "9886201234",
                DateOfBirth = new DateTime(2004, 10, 3),
                Gender = "Male",
                Program = "B.Com.",
                CurrentSemester = 2,
                LifecycleStatus = "ON_LEAVE",
                Address = "Electronic City Phase 1, Bengaluru, Karnataka",
                GuardianName = "Vijay Deshmukh",
                GuardianPhone = "9886209876",
                AdmissionDate = new DateTime(2025, 7, 25, 14, 15, 0, DateTimeKind.Utc)
            },
            new() {
                Id = "670f00010000000000000011",
                AdmissionNumber = "ADM-2026-00162",
                RollNumber = null,
                FirstName = "Pooja",
                LastName = "Sharma",
                Email = "pooja.sharma@student.vit.edu",
                Phone = "9611212345",
                DateOfBirth = new DateTime(2005, 5, 19),
                Gender = "Female",
                Program = "BBA",
                CurrentSemester = 1,
                LifecycleStatus = "ADMITTED",
                Address = "Hebbal, Bengaluru, Karnataka",
                GuardianName = "Rajesh Sharma",
                GuardianPhone = "9611298765",
                AdmissionDate = new DateTime(2026, 7, 2, 10, 45, 0, DateTimeKind.Utc)
            },
            new() {
                Id = "670f00010000000000000012",
                AdmissionNumber = "ADM-2023-00018",
                RollNumber = "23BCA044",
                FirstName = "Vikram",
                LastName = "Verma",
                Email = "vikram.verma@student.vit.edu",
                Phone = "9480223456",
                DateOfBirth = new DateTime(2003, 2, 11),
                Gender = "Male",
                Program = "BCA",
                CurrentSemester = 6,
                LifecycleStatus = "ACTIVE",
                Address = "BTM Layout 2nd Stage, Bengaluru, Karnataka",
                GuardianName = "Ashok Verma",
                GuardianPhone = "9480287654",
                AdmissionDate = new DateTime(2023, 7, 1, 11, 0, 0, DateTimeKind.Utc)
            }
        };
    }
}
