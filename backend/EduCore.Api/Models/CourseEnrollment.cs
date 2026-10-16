using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EduCore.Api.Models;

public class CourseEnrollment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string RegistrationId { get; set; } = string.Empty;

    public string StudentId { get; set; } = string.Empty;

    public string CourseId { get; set; } = string.Empty;

    public string CourseCode { get; set; } = string.Empty;

    public string CourseTitle { get; set; } = string.Empty;

    public int Credits { get; set; } = 4;

    public string CourseType { get; set; } = "CORE";

    public string AcademicPeriodCode { get; set; } = "2026-ODD";

    public string Status { get; set; } = "ENROLLED"; // ENROLLED, WITHDRAWN, COMPLETED

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    public DateTime? WithdrawnAt { get; set; }
}
