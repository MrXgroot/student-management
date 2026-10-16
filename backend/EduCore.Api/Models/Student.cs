using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EduCore.Api.Models;

public class Student
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string AdmissionNumber { get; set; } = string.Empty;

    public string? RollNumber { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string Gender { get; set; } = "Other";

    public string Program { get; set; } = "BCA";

    public int CurrentSemester { get; set; } = 1;

    public string LifecycleStatus { get; set; } = "ADMITTED";

    public string Address { get; set; } = string.Empty;

    public string GuardianName { get; set; } = string.Empty;

    public string GuardianPhone { get; set; } = string.Empty;

    public DateTime AdmissionDate { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
