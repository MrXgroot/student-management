using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EduCore.Api.Models;

public class AttendanceRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string AcademicPeriodCode { get; set; } = "2026-ODD";

    public string CourseId { get; set; } = string.Empty;

    public string CourseCode { get; set; } = string.Empty;

    public string CourseTitle { get; set; } = string.Empty;

    public string StudentId { get; set; } = string.Empty;

    public string AdmissionNumber { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public string? RollNumber { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ClassDate { get; set; } = DateTime.UtcNow.Date;

    public string ClassDateString { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    public string Status { get; set; } = "PRESENT"; // PRESENT, ABSENT

    public string? Remarks { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
