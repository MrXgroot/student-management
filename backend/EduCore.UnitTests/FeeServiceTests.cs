using EduCore.Api.Data;
using EduCore.Api.Models;
using EduCore.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EduCore.UnitTests;

public class FeeServiceTests
{
    private readonly IStudentService _studentService;
    private readonly IAcademicService _academicService;
    private readonly IFeeService _feeService;

    public FeeServiceTests()
    {
        var context = new MongoDbContext(new MongoDbOptions
        {
            ConnectionString = "",
            DatabaseName = "educore_mvp_test",
            AllowInMemoryFallback = true
        }, NullLogger<MongoDbContext>.Instance);

        _studentService = new StudentService(context, NullLogger<StudentService>.Instance);
        _academicService = new AcademicService(context, NullLogger<AcademicService>.Instance);
        _feeService = new FeeService(context, _studentService, _academicService, NullLogger<FeeService>.Instance);
    }

    private async Task<Student> CreateTestStudentAsync(string program = "BCA", int semester = 1)
    {
        var unique = Guid.NewGuid().ToString("N")[..6];
        var dto = new CreateStudentDto
        {
            FirstName = "FeeStudent" + unique,
            LastName = "Test",
            Email = $"fee.{unique}@student.vit.edu",
            Phone = "98" + new Random().Next(10000000, 99999999),
            Program = program,
            CurrentSemester = semester,
            Gender = "Other"
        };
        return await _studentService.CreateStudentAsync(dto);
    }

    [Fact]
    public async Task GetProgramFeeStructuresAsync_ReturnsConfiguredStructures()
    {
        var structures = await _feeService.GetProgramFeeStructuresAsync("2026-ODD");

        Assert.NotNull(structures);
        Assert.NotEmpty(structures);
        var bca = structures.FirstOrDefault(s => s.ProgramCode == "BCA");
        Assert.NotNull(bca);
        Assert.Equal(5000000, bca.TotalFeePaise); // ₹50,000 = 5,000,000 paise
    }

    [Fact]
    public async Task GetFeeOverviewAsync_IncludesStudentFeeRecords()
    {
        var student = await CreateTestStudentAsync("BCA", 1);
        var overview = await _feeService.GetFeeOverviewAsync("2026-ODD", "BCA", "ALL", null);

        Assert.NotNull(overview);
        Assert.True(overview.TotalBilledPaise > 0);
        Assert.Contains(overview.Students, s => s.StudentId == student.Id);

        var studentFee = overview.Students.First(s => s.StudentId == student.Id);
        Assert.Equal(5000000, studentFee.TotalFeePaise);
        Assert.Equal(50000.00m, studentFee.TotalFeeRupees);
    }

    [Fact]
    public async Task RecordPaymentAsync_PartialPayment_UpdatesRecordAndCreatesReceipt()
    {
        var student = await CreateTestStudentAsync("BCA", 1);
        var overview = await _feeService.GetFeeOverviewAsync("2026-ODD", "BCA", "ALL", null);
        var initialRecord = overview.Students.First(s => s.StudentId == student.Id);

        var paymentAmountPaise = 2000000; // ₹20,000
        var paymentDto = new RecordPaymentDto
        {
            StudentId = student.Id,
            AcademicPeriodCode = "2026-ODD",
            AmountPaise = paymentAmountPaise,
            PaymentMethod = "UPI",
            TransactionReference = "UPI/TEST/12345",
            Remarks = "First partial payment"
        };

        var receipt = await _feeService.RecordPaymentAsync(paymentDto);

        Assert.NotNull(receipt);
        Assert.StartsWith("REC-", receipt.ReceiptNumber);
        Assert.Equal(paymentAmountPaise, receipt.AmountPaise);
        Assert.Equal(20000.00m, receipt.AmountRupees);
        Assert.Equal("UPI", receipt.PaymentMethod);

        // Verify updated student fee detail
        var detail = await _feeService.GetStudentFeeDetailAsync(student.Id, "2026-ODD");
        Assert.NotNull(detail);
        Assert.Equal(paymentAmountPaise, detail.FeeRecord.PaidAmountPaise);
        Assert.Equal(initialRecord.TotalFeePaise - paymentAmountPaise, detail.FeeRecord.BalanceAmountPaise);
        Assert.Equal("PARTIAL", detail.FeeRecord.Status);
        Assert.Contains(detail.Payments, p => p.ReceiptNumber == receipt.ReceiptNumber);
    }

    [Fact]
    public async Task RecordPaymentAsync_FullPayment_TransitionsStatusToPaid()
    {
        var student = await CreateTestStudentAsync("B.Com.", 1);
        var detailBefore = await _feeService.GetStudentFeeDetailAsync(student.Id, "2026-ODD");
        Assert.NotNull(detailBefore);

        var fullBalance = detailBefore.FeeRecord.BalanceAmountPaise;

        var receipt = await _feeService.RecordPaymentAsync(new RecordPaymentDto
        {
            StudentId = student.Id,
            AcademicPeriodCode = "2026-ODD",
            AmountPaise = fullBalance,
            PaymentMethod = "BANK_TRANSFER",
            TransactionReference = "NEFT-998877",
            Remarks = "Full clearance"
        });

        var detailAfter = await _feeService.GetStudentFeeDetailAsync(student.Id, "2026-ODD");
        Assert.NotNull(detailAfter);
        Assert.Equal(0, detailAfter.FeeRecord.BalanceAmountPaise);
        Assert.Equal("PAID", detailAfter.FeeRecord.Status);
    }

    [Fact]
    public async Task RecordPaymentAsync_Overpayment_ThrowsInvalidOperationException()
    {
        var student = await CreateTestStudentAsync("BCA", 1);
        var detail = await _feeService.GetStudentFeeDetailAsync(student.Id, "2026-ODD");
        Assert.NotNull(detail);

        var overpayment = detail.FeeRecord.BalanceAmountPaise + 500000; // ₹5,000 extra

        var paymentDto = new RecordPaymentDto
        {
            StudentId = student.Id,
            AcademicPeriodCode = "2026-ODD",
            AmountPaise = overpayment,
            PaymentMethod = "UPI"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _feeService.RecordPaymentAsync(paymentDto));
        Assert.Contains("exceeds outstanding balance", ex.Message);
    }

    [Fact]
    public async Task RecordPaymentAsync_ZeroOrNegativeAmount_ThrowsArgumentException()
    {
        var student = await CreateTestStudentAsync("BCA", 1);

        var paymentDto = new RecordPaymentDto
        {
            StudentId = student.Id,
            AcademicPeriodCode = "2026-ODD",
            AmountPaise = 0,
            PaymentMethod = "CASH"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _feeService.RecordPaymentAsync(paymentDto));
    }

    [Fact]
    public async Task UpdateProgramFeeStructureAsync_UpdatesFeeConfiguration()
    {
        var updateDto = new UpdateProgramFeeDto
        {
            TuitionFeePaise = 4600000,
            OtherFeesPaise = 400000
        };

        var updated = await _feeService.UpdateProgramFeeStructureAsync("BCA", updateDto, "2026-ODD");

        Assert.Equal(5000000, updated.TotalFeePaise);
        Assert.Equal(4600000, updated.TuitionFeePaise);
        Assert.Equal(400000, updated.OtherFeesPaise);
    }
}
