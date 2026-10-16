using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EduCore.Api.Models;

public class Course
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int Credits { get; set; } = 4;

    public string ProgramCode { get; set; } = "BCA";

    public int Semester { get; set; } = 1;

    public string Department { get; set; } = "Computer Applications";

    public string CourseType { get; set; } = "CORE"; // CORE, ELECTIVE, LAB

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = "ACTIVE"; // ACTIVE, INACTIVE

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
