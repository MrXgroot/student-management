using System.ComponentModel.DataAnnotations;

namespace EduCore.Api.Models;

public class RecordPaymentDto
{
    [Required(ErrorMessage = "Student ID is required")]
    public string StudentId { get; set; } = string.Empty;

    public string AcademicPeriodCode { get; set; } = "2026-ODD";

    [Required(ErrorMessage = "Payment amount in paise is required")]
    [Range(100, 100000000, ErrorMessage = "Payment amount must be at least ₹1.00 (100 paise) and cannot exceed ₹10,00,000")]
    public long AmountPaise { get; set; }

    [Required(ErrorMessage = "Payment method is required")]
    [RegularExpression(@"^(UPI|BANK_TRANSFER|CASH|CHEQUE|DEMAND_DRAFT)$", 
        ErrorMessage = "Payment method must be UPI, BANK_TRANSFER, CASH, CHEQUE, or DEMAND_DRAFT")]
    public string PaymentMethod { get; set; } = "UPI";

    public string? TransactionReference { get; set; }

    public string? Remarks { get; set; }
}

public class UpdateProgramFeeDto
{
    [Range(0, 100000000, ErrorMessage = "Tuition fee must be non-negative")]
    public long TuitionFeePaise { get; set; }

    [Range(0, 100000000, ErrorMessage = "Other fees must be non-negative")]
    public long OtherFeesPaise { get; set; }
}

public class StudentFeeSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string? RollNumber { get; set; }
    public string ProgramCode { get; set; } = string.Empty;
    public int CurrentSemester { get; set; } = 1;
    public string AcademicPeriodCode { get; set; } = "2026-ODD";

    public long TotalFeePaise { get; set; }
    public long PaidAmountPaise { get; set; }
    public long BalanceAmountPaise { get; set; }

    public decimal TotalFeeRupees => TotalFeePaise / 100m;
    public decimal PaidAmountRupees => PaidAmountPaise / 100m;
    public decimal BalanceAmountRupees => BalanceAmountPaise / 100m;

    public string Status { get; set; } = "UNPAID";
    public DateTime? LastPaymentDate { get; set; }
}

public class FeeOverviewDto
{
    public string AcademicPeriodCode { get; set; } = "2026-ODD";
    public long TotalBilledPaise { get; set; }
    public long TotalCollectedPaise { get; set; }
    public long TotalOutstandingPaise { get; set; }

    public decimal TotalBilledRupees => TotalBilledPaise / 100m;
    public decimal TotalCollectedRupees => TotalCollectedPaise / 100m;
    public decimal TotalOutstandingRupees => TotalOutstandingPaise / 100m;

    public int CollectionRatePercent => TotalBilledPaise > 0 
        ? (int)Math.Round((double)TotalCollectedPaise / TotalBilledPaise * 100) 
        : 0;

    public int TotalStudents { get; set; }
    public int PaidStudentsCount { get; set; }
    public int PartialStudentsCount { get; set; }
    public int UnpaidStudentsCount { get; set; }

    public List<StudentFeeSummaryDto> Students { get; set; } = new();
}

public class FeePaymentDto
{
    public string Id { get; set; } = string.Empty;
    public string ReceiptNumber { get; set; } = string.Empty;
    public string FeeRecordId { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string ProgramCode { get; set; } = string.Empty;
    public string AcademicPeriodCode { get; set; } = "2026-ODD";
    public long AmountPaise { get; set; }
    public decimal AmountRupees => AmountPaise / 100m;
    public string PaymentMethod { get; set; } = "UPI";
    public string? TransactionReference { get; set; }
    public string? Remarks { get; set; }
    public string RecordedBy { get; set; } = "Accounts Office";
    public DateTime PaymentDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StudentFeeDetailDto
{
    public StudentFeeSummaryDto FeeRecord { get; set; } = new();
    public List<FeePaymentDto> Payments { get; set; } = new();
}
