using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EduCore.Api.Models;

public class ProgramFeeStructure
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string ProgramCode { get; set; } = string.Empty;

    public string ProgramName { get; set; } = string.Empty;

    public string AcademicPeriodCode { get; set; } = "2026-ODD";

    // Amounts in integer paise (e.g. 5000000 paise = ₹50,000)
    public long TuitionFeePaise { get; set; } = 4500000;

    public long OtherFeesPaise { get; set; } = 500000;

    public long TotalFeePaise => TuitionFeePaise + OtherFeesPaise;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class StudentFeeRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string StudentId { get; set; } = string.Empty;

    public string AdmissionNumber { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public string? RollNumber { get; set; }

    public string ProgramCode { get; set; } = string.Empty;

    public int CurrentSemester { get; set; } = 1;

    public string AcademicPeriodCode { get; set; } = "2026-ODD";

    // Money in integer paise
    public long TotalFeePaise { get; set; }

    public long PaidAmountPaise { get; set; } = 0;

    public long BalanceAmountPaise => Math.Max(0, TotalFeePaise - PaidAmountPaise);

    // Status: UNPAID, PARTIAL, PAID
    public string Status
    {
        get
        {
            if (PaidAmountPaise <= 0) return "UNPAID";
            if (PaidAmountPaise >= TotalFeePaise) return "PAID";
            return "PARTIAL";
        }
    }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? LastPaymentDate { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class FeePayment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string ReceiptNumber { get; set; } = string.Empty;

    public string FeeRecordId { get; set; } = string.Empty;

    public string StudentId { get; set; } = string.Empty;

    public string AdmissionNumber { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public string ProgramCode { get; set; } = string.Empty;

    public string AcademicPeriodCode { get; set; } = "2026-ODD";

    // Amount in integer paise
    public long AmountPaise { get; set; }

    // PaymentMethod: UPI, BANK_TRANSFER, CASH, CHEQUE, DEMAND_DRAFT
    public string PaymentMethod { get; set; } = "UPI";

    public string? TransactionReference { get; set; }

    public string? Remarks { get; set; }

    public string RecordedBy { get; set; } = "Accounts Office";

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
