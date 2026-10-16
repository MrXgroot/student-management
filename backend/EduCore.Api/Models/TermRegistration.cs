using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EduCore.Api.Models;

public class TermRegistration
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string StudentId { get; set; } = string.Empty;

    public string AdmissionNumber { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public string ProgramCode { get; set; } = string.Empty;

    public string AcademicPeriodId { get; set; } = string.Empty;

    public string AcademicPeriodCode { get; set; } = "2026-ODD";

    public int Semester { get; set; } = 1;

    public string AcademicStanding { get; set; } = "REGULAR"; // REGULAR, PROBATION

    public string Status { get; set; } = "REGISTERED"; // REGISTERED, CONFIRMED, WITHDRAWN

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
