using System.Collections.Concurrent;
using EduCore.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EduCore.Api.Services;

public class FeeService : IFeeService
{
    private readonly ILogger<FeeService> _logger;
    private readonly IStudentService _studentService;
    private readonly IAcademicService _academicService;

    private readonly IMongoCollection<ProgramFeeStructure>? _structureCollection;
    private readonly IMongoCollection<StudentFeeRecord>? _recordCollection;
    private readonly IMongoCollection<FeePayment>? _paymentCollection;

    private readonly ConcurrentDictionary<string, ProgramFeeStructure> _inMemoryStructures = new();
    private readonly ConcurrentDictionary<string, StudentFeeRecord> _inMemoryRecords = new();
    private readonly ConcurrentDictionary<string, FeePayment> _inMemoryPayments = new();

    private bool _isMongoDbActive;

    public bool IsMongoDbActive => _isMongoDbActive;

    public FeeService(
        IConfiguration config,
        IStudentService studentService,
        IAcademicService academicService,
        ILogger<FeeService> logger)
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

            _structureCollection = database.GetCollection<ProgramFeeStructure>("programFeeStructures");
            _recordCollection = database.GetCollection<StudentFeeRecord>("studentFeeRecords");
            _paymentCollection = database.GetCollection<FeePayment>("feePayments");

            database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(2500);
            _isMongoDbActive = true;
            _logger.LogInformation("FeeService connected to MongoDB at {Conn}, Database: {Db}", connectionString, databaseName);

            // 1. Program Fee Structure unique index: { ProgramCode: 1, AcademicPeriodCode: 1 }
            var structKeys = Builders<ProgramFeeStructure>.IndexKeys
                .Ascending(s => s.ProgramCode)
                .Ascending(s => s.AcademicPeriodCode);
            _structureCollection.Indexes.CreateOne(new CreateIndexModel<ProgramFeeStructure>(structKeys, new CreateIndexOptions { Unique = true }));

            // 2. Student Fee Record unique index: { StudentId: 1, AcademicPeriodCode: 1 }
            var recordKeys = Builders<StudentFeeRecord>.IndexKeys
                .Ascending(r => r.StudentId)
                .Ascending(r => r.AcademicPeriodCode);
            _recordCollection.Indexes.CreateOne(new CreateIndexModel<StudentFeeRecord>(recordKeys, new CreateIndexOptions { Unique = true }));

            // 3. Fee Payment unique receipt index: { ReceiptNumber: 1 }
            var receiptKeys = Builders<FeePayment>.IndexKeys.Ascending(p => p.ReceiptNumber);
            _paymentCollection.Indexes.CreateOne(new CreateIndexModel<FeePayment>(receiptKeys, new CreateIndexOptions { Unique = true }));

            // 4. Payment student index
            var paymentStudentKeys = Builders<FeePayment>.IndexKeys
                .Ascending(p => p.StudentId)
                .Descending(p => p.PaymentDate);
            _paymentCollection.Indexes.CreateOne(new CreateIndexModel<FeePayment>(paymentStudentKeys));
        }
        catch (Exception ex)
        {
            _isMongoDbActive = false;
            _structureCollection = null;
            _recordCollection = null;
            _paymentCollection = null;
            _logger.LogWarning(ex, "MongoDB unavailable for FeeService. Using resilient in-memory storage.");
        }

        // Initialize default fee structures and demo seed data
        InitializeFeeDataAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeFeeDataAsync()
    {
        try
        {
            // Default program fee structures
            var defaults = new List<ProgramFeeStructure>
            {
                new() { ProgramCode = "BCA", ProgramName = "Bachelor of Computer Applications", AcademicPeriodCode = "2026-ODD", TuitionFeePaise = 4500000, OtherFeesPaise = 500000 },
                new() { ProgramCode = "B.Sc. Computer Science", ProgramName = "Bachelor of Science in Computer Science", AcademicPeriodCode = "2026-ODD", TuitionFeePaise = 4000000, OtherFeesPaise = 500000 },
                new() { ProgramCode = "B.Com.", ProgramName = "Bachelor of Commerce", AcademicPeriodCode = "2026-ODD", TuitionFeePaise = 3200000, OtherFeesPaise = 400000 },
                new() { ProgramCode = "BBA", ProgramName = "Bachelor of Business Administration", AcademicPeriodCode = "2026-ODD", TuitionFeePaise = 4800000, OtherFeesPaise = 600000 },
                new() { ProgramCode = "MCA", ProgramName = "Master of Computer Applications", AcademicPeriodCode = "2026-ODD", TuitionFeePaise = 6000000, OtherFeesPaise = 800000 },
                new() { ProgramCode = "MBA", ProgramName = "Master of Business Administration", AcademicPeriodCode = "2026-ODD", TuitionFeePaise = 7500000, OtherFeesPaise = 1000000 }
            };

            foreach (var def in defaults)
            {
                var key = $"{def.ProgramCode}_{def.AcademicPeriodCode}";
                _inMemoryStructures.TryAdd(key, def);

                if (_isMongoDbActive && _structureCollection != null)
                {
                    var filter = Builders<ProgramFeeStructure>.Filter.Eq(s => s.ProgramCode, def.ProgramCode)
                               & Builders<ProgramFeeStructure>.Filter.Eq(s => s.AcademicPeriodCode, def.AcademicPeriodCode);
                    var existing = await _structureCollection.Find(filter).FirstOrDefaultAsync();
                    if (existing == null)
                    {
                        await _structureCollection.InsertOneAsync(def);
                    }
                }
            }

            // Sync fee records for students
            await SyncStudentFeeRecordsAsync("2026-ODD");

            // Seed demo payments if no payments exist
            var paymentsCount = await GetPaymentCountAsync();
            if (paymentsCount == 0)
            {
                var students = await _studentService.GetStudentsAsync();
                var firstStudent = students.FirstOrDefault();
                if (firstStudent != null)
                {
                    // Record a partial payment of ₹25,000 (2500000 paise)
                    await RecordPaymentAsync(new RecordPaymentDto
                    {
                        StudentId = firstStudent.Id,
                        AcademicPeriodCode = "2026-ODD",
                        AmountPaise = 2500000,
                        PaymentMethod = "UPI",
                        TransactionReference = "UPI/2026/98234812",
                        Remarks = "First installment (Tuition Fee)"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize fee demo data.");
        }
    }

    private async Task<long> GetPaymentCountAsync()
    {
        if (_isMongoDbActive && _paymentCollection != null)
        {
            try
            {
                return await _paymentCollection.CountDocumentsAsync(Builders<FeePayment>.Filter.Empty);
            }
            catch
            {
                return _inMemoryPayments.Count;
            }
        }
        return _inMemoryPayments.Count;
    }

    private async Task<ProgramFeeStructure> GetOrCreateStructureAsync(string programCode, string period)
    {
        var key = $"{programCode}_{period}";
        if (_inMemoryStructures.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (_isMongoDbActive && _structureCollection != null)
        {
            try
            {
                var filter = Builders<ProgramFeeStructure>.Filter.Eq(s => s.ProgramCode, programCode)
                           & Builders<ProgramFeeStructure>.Filter.Eq(s => s.AcademicPeriodCode, period);
                var doc = await _structureCollection.Find(filter).FirstOrDefaultAsync();
                if (doc != null)
                {
                    _inMemoryStructures[key] = doc;
                    return doc;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB structure lookup failed for {Prog} {Period}.", programCode, period);
            }
        }

        // Fallback default
        var fallback = new ProgramFeeStructure
        {
            ProgramCode = programCode,
            ProgramName = programCode,
            AcademicPeriodCode = period,
            TuitionFeePaise = 4500000,
            OtherFeesPaise = 500000
        };
        _inMemoryStructures[key] = fallback;
        return fallback;
    }

    private async Task SyncStudentFeeRecordsAsync(string period)
    {
        var students = await _studentService.GetStudentsAsync();
        foreach (var student in students)
        {
            var structDef = await GetOrCreateStructureAsync(student.Program, period);
            var memKey = $"{student.Id}_{period}";

            if (_isMongoDbActive && _recordCollection != null)
            {
                try
                {
                    var filter = Builders<StudentFeeRecord>.Filter.Eq(r => r.StudentId, student.Id)
                               & Builders<StudentFeeRecord>.Filter.Eq(r => r.AcademicPeriodCode, period);
                    var existing = await _recordCollection.Find(filter).FirstOrDefaultAsync();
                    if (existing == null)
                    {
                        var newRecord = new StudentFeeRecord
                        {
                            StudentId = student.Id,
                            AdmissionNumber = student.AdmissionNumber,
                            StudentName = student.FullName,
                            RollNumber = student.RollNumber,
                            ProgramCode = student.Program,
                            CurrentSemester = student.CurrentSemester,
                            AcademicPeriodCode = period,
                            TotalFeePaise = structDef.TotalFeePaise,
                            PaidAmountPaise = 0,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        await _recordCollection.InsertOneAsync(newRecord);
                        _inMemoryRecords[memKey] = newRecord;
                    }
                    else
                    {
                        _inMemoryRecords[memKey] = existing;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync student fee record for {StudentId}.", student.Id);
                }
            }
            else
            {
                if (!_inMemoryRecords.ContainsKey(memKey))
                {
                    _inMemoryRecords[memKey] = new StudentFeeRecord
                    {
                        StudentId = student.Id,
                        AdmissionNumber = student.AdmissionNumber,
                        StudentName = student.FullName,
                        RollNumber = student.RollNumber,
                        ProgramCode = student.Program,
                        CurrentSemester = student.CurrentSemester,
                        AcademicPeriodCode = period,
                        TotalFeePaise = structDef.TotalFeePaise,
                        PaidAmountPaise = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                }
            }
        }
    }

    public async Task<List<ProgramFeeStructure>> GetProgramFeeStructuresAsync(string? academicPeriodCode = null)
    {
        var period = string.IsNullOrWhiteSpace(academicPeriodCode) ? "2026-ODD" : academicPeriodCode;

        if (_isMongoDbActive && _structureCollection != null)
        {
            try
            {
                var filter = Builders<ProgramFeeStructure>.Filter.Eq(s => s.AcademicPeriodCode, period);
                var list = await _structureCollection.Find(filter).ToListAsync();
                if (list != null && list.Count > 0) return list;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB structure query failed.");
            }
        }

        return _inMemoryStructures.Values
            .Where(s => s.AcademicPeriodCode.Equals(period, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<ProgramFeeStructure> UpdateProgramFeeStructureAsync(string programCode, UpdateProgramFeeDto dto, string? academicPeriodCode = null)
    {
        var period = string.IsNullOrWhiteSpace(academicPeriodCode) ? "2026-ODD" : academicPeriodCode;
        var existing = await GetOrCreateStructureAsync(programCode, period);

        existing.TuitionFeePaise = dto.TuitionFeePaise;
        existing.OtherFeesPaise = dto.OtherFeesPaise;
        existing.UpdatedAt = DateTime.UtcNow;

        var key = $"{programCode}_{period}";
        _inMemoryStructures[key] = existing;

        if (_isMongoDbActive && _structureCollection != null)
        {
            try
            {
                var filter = Builders<ProgramFeeStructure>.Filter.Eq(s => s.ProgramCode, programCode)
                           & Builders<ProgramFeeStructure>.Filter.Eq(s => s.AcademicPeriodCode, period);
                await _structureCollection.ReplaceOneAsync(filter, existing, new ReplaceOptions { IsUpsert = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB structure update failed for {Prog}.", programCode);
            }
        }

        return existing;
    }

    public async Task<FeeOverviewDto> GetFeeOverviewAsync(string? period, string? program, string? status, string? search)
    {
        var term = string.IsNullOrWhiteSpace(period) || period.Equals("ALL", StringComparison.OrdinalIgnoreCase) 
            ? "2026-ODD" : period;

        // Ensure all students have a record
        await SyncStudentFeeRecordsAsync(term);

        List<StudentFeeRecord> allRecords;
        if (_isMongoDbActive && _recordCollection != null)
        {
            try
            {
                var filter = Builders<StudentFeeRecord>.Filter.Eq(r => r.AcademicPeriodCode, term);
                allRecords = await _recordCollection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB record overview lookup failed.");
                allRecords = _inMemoryRecords.Values.Where(r => r.AcademicPeriodCode == term).ToList();
            }
        }
        else
        {
            allRecords = _inMemoryRecords.Values.Where(r => r.AcademicPeriodCode == term).ToList();
        }

        // Apply filters
        var query = allRecords.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(program) && !program.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.ProgramCode.Equals(program, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            query = query.Where(r =>
                r.StudentName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.AdmissionNumber.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (r.RollNumber != null && r.RollNumber.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        var filteredList = query.OrderBy(r => r.StudentName).ToList();

        var summaries = filteredList.Select(r => new StudentFeeSummaryDto
        {
            Id = r.Id,
            StudentId = r.StudentId,
            AdmissionNumber = r.AdmissionNumber,
            StudentName = r.StudentName,
            RollNumber = r.RollNumber,
            ProgramCode = r.ProgramCode,
            CurrentSemester = r.CurrentSemester,
            AcademicPeriodCode = r.AcademicPeriodCode,
            TotalFeePaise = r.TotalFeePaise,
            PaidAmountPaise = r.PaidAmountPaise,
            BalanceAmountPaise = r.BalanceAmountPaise,
            Status = r.Status,
            LastPaymentDate = r.LastPaymentDate
        }).ToList();

        var totalBilled = filteredList.Sum(r => r.TotalFeePaise);
        var totalPaid = filteredList.Sum(r => r.PaidAmountPaise);
        var totalBalance = filteredList.Sum(r => r.BalanceAmountPaise);

        return new FeeOverviewDto
        {
            AcademicPeriodCode = term,
            TotalBilledPaise = totalBilled,
            TotalCollectedPaise = totalPaid,
            TotalOutstandingPaise = totalBalance,
            TotalStudents = filteredList.Count,
            PaidStudentsCount = filteredList.Count(r => r.Status == "PAID"),
            PartialStudentsCount = filteredList.Count(r => r.Status == "PARTIAL"),
            UnpaidStudentsCount = filteredList.Count(r => r.Status == "UNPAID"),
            Students = summaries
        };
    }

    public async Task<StudentFeeDetailDto?> GetStudentFeeDetailAsync(string studentId, string? academicPeriodCode = null)
    {
        var period = string.IsNullOrWhiteSpace(academicPeriodCode) ? "2026-ODD" : academicPeriodCode;
        await SyncStudentFeeRecordsAsync(period);

        StudentFeeRecord? record = null;
        if (_isMongoDbActive && _recordCollection != null)
        {
            try
            {
                var filter = Builders<StudentFeeRecord>.Filter.Eq(r => r.StudentId, studentId)
                           & Builders<StudentFeeRecord>.Filter.Eq(r => r.AcademicPeriodCode, period);
                record = await _recordCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB student fee lookup failed for {Id}.", studentId);
            }
        }

        if (record == null)
        {
            var memKey = $"{studentId}_{period}";
            _inMemoryRecords.TryGetValue(memKey, out record);
        }

        if (record == null)
        {
            return null;
        }

        var payments = await GetPaymentHistoryAsync(studentId, period);

        return new StudentFeeDetailDto
        {
            FeeRecord = new StudentFeeSummaryDto
            {
                Id = record.Id,
                StudentId = record.StudentId,
                AdmissionNumber = record.AdmissionNumber,
                StudentName = record.StudentName,
                RollNumber = record.RollNumber,
                ProgramCode = record.ProgramCode,
                CurrentSemester = record.CurrentSemester,
                AcademicPeriodCode = record.AcademicPeriodCode,
                TotalFeePaise = record.TotalFeePaise,
                PaidAmountPaise = record.PaidAmountPaise,
                BalanceAmountPaise = record.BalanceAmountPaise,
                Status = record.Status,
                LastPaymentDate = record.LastPaymentDate
            },
            Payments = payments
        };
    }

    private string GenerateReceiptNumber()
    {
        var year = DateTime.UtcNow.Year;
        var rand = Random.Shared.Next(1000, 9999);
        var suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return $"REC-{year}-{rand}{suffix}";
    }

    public async Task<FeePaymentDto> RecordPaymentAsync(RecordPaymentDto dto)
    {
        if (dto.AmountPaise <= 0)
        {
            throw new ArgumentException("Payment amount must be greater than zero.");
        }

        var student = await _studentService.GetStudentByIdAsync(dto.StudentId);
        if (student == null)
        {
            throw new KeyNotFoundException($"Student with ID '{dto.StudentId}' not found.");
        }

        var period = string.IsNullOrWhiteSpace(dto.AcademicPeriodCode) ? "2026-ODD" : dto.AcademicPeriodCode;
        await SyncStudentFeeRecordsAsync(period);

        // Fetch student's fee record
        StudentFeeRecord? record = null;
        var memKey = $"{student.Id}_{period}";

        if (_isMongoDbActive && _recordCollection != null)
        {
            var filter = Builders<StudentFeeRecord>.Filter.Eq(r => r.StudentId, student.Id)
                       & Builders<StudentFeeRecord>.Filter.Eq(r => r.AcademicPeriodCode, period);
            record = await _recordCollection.Find(filter).FirstOrDefaultAsync();
        }

        if (record == null)
        {
            _inMemoryRecords.TryGetValue(memKey, out record);
        }

        if (record == null)
        {
            throw new KeyNotFoundException($"Fee account not found for student '{student.FullName}' in period '{period}'.");
        }

        // Validate overpayment: amount cannot exceed outstanding balance
        if (dto.AmountPaise > record.BalanceAmountPaise)
        {
            var requestedRupees = dto.AmountPaise / 100m;
            var balanceRupees = record.BalanceAmountPaise / 100m;
            throw new InvalidOperationException(
                $"Payment amount of ₹{requestedRupees:N2} exceeds outstanding balance of ₹{balanceRupees:N2}. Overpayment is not allowed.");
        }

        var receiptNo = GenerateReceiptNumber();
        var now = DateTime.UtcNow;

        var payment = new FeePayment
        {
            ReceiptNumber = receiptNo,
            FeeRecordId = record.Id,
            StudentId = student.Id,
            AdmissionNumber = student.AdmissionNumber,
            StudentName = student.FullName,
            ProgramCode = student.Program,
            AcademicPeriodCode = period,
            AmountPaise = dto.AmountPaise,
            PaymentMethod = dto.PaymentMethod.Trim().ToUpperInvariant(),
            TransactionReference = dto.TransactionReference,
            Remarks = dto.Remarks,
            RecordedBy = "Staff / Cashier",
            PaymentDate = now,
            CreatedAt = now
        };

        // Update fee record
        record.PaidAmountPaise += dto.AmountPaise;
        record.LastPaymentDate = now;
        record.UpdatedAt = now;

        if (_isMongoDbActive && _paymentCollection != null && _recordCollection != null)
        {
            try
            {
                await _paymentCollection.InsertOneAsync(payment);

                var filter = Builders<StudentFeeRecord>.Filter.Eq(r => r.StudentId, student.Id)
                           & Builders<StudentFeeRecord>.Filter.Eq(r => r.AcademicPeriodCode, period);
                await _recordCollection.ReplaceOneAsync(filter, record, new ReplaceOptions { IsUpsert = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB payment insert failed, storing in-memory.");
            }
        }

        _inMemoryPayments[payment.Id] = payment;
        _inMemoryRecords[memKey] = record;

        return new FeePaymentDto
        {
            Id = payment.Id,
            ReceiptNumber = payment.ReceiptNumber,
            FeeRecordId = payment.FeeRecordId,
            StudentId = payment.StudentId,
            AdmissionNumber = payment.AdmissionNumber,
            StudentName = payment.StudentName,
            ProgramCode = payment.ProgramCode,
            AcademicPeriodCode = payment.AcademicPeriodCode,
            AmountPaise = payment.AmountPaise,
            PaymentMethod = payment.PaymentMethod,
            TransactionReference = payment.TransactionReference,
            Remarks = payment.Remarks,
            RecordedBy = payment.RecordedBy,
            PaymentDate = payment.PaymentDate,
            CreatedAt = payment.CreatedAt
        };
    }

    public async Task<List<FeePaymentDto>> GetPaymentHistoryAsync(string? studentId = null, string? period = null, int limit = 50)
    {
        List<FeePayment> payments;
        if (_isMongoDbActive && _paymentCollection != null)
        {
            try
            {
                var builder = Builders<FeePayment>.Filter;
                var filter = builder.Empty;

                if (!string.IsNullOrWhiteSpace(studentId))
                {
                    filter &= builder.Eq(p => p.StudentId, studentId);
                }

                if (!string.IsNullOrWhiteSpace(period) && !period.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    filter &= builder.Eq(p => p.AcademicPeriodCode, period);
                }

                payments = await _paymentCollection
                    .Find(filter)
                    .SortByDescending(p => p.PaymentDate)
                    .Limit(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MongoDB payment history query failed.");
                payments = _inMemoryPayments.Values.ToList();
            }
        }
        else
        {
            payments = _inMemoryPayments.Values.ToList();
        }

        var q = payments.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(studentId))
        {
            q = q.Where(p => p.StudentId == studentId);
        }
        if (!string.IsNullOrWhiteSpace(period) && !period.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(p => p.AcademicPeriodCode.Equals(period, StringComparison.OrdinalIgnoreCase));
        }

        return q.OrderByDescending(p => p.PaymentDate)
            .Take(limit)
            .Select(p => new FeePaymentDto
            {
                Id = p.Id,
                ReceiptNumber = p.ReceiptNumber,
                FeeRecordId = p.FeeRecordId,
                StudentId = p.StudentId,
                AdmissionNumber = p.AdmissionNumber,
                StudentName = p.StudentName,
                ProgramCode = p.ProgramCode,
                AcademicPeriodCode = p.AcademicPeriodCode,
                AmountPaise = p.AmountPaise,
                PaymentMethod = p.PaymentMethod,
                TransactionReference = p.TransactionReference,
                Remarks = p.Remarks,
                RecordedBy = p.RecordedBy,
                PaymentDate = p.PaymentDate,
                CreatedAt = p.CreatedAt
            }).ToList();
    }
}
