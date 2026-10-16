using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EduCore.Api.Models;

public class ProgramInfo
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Award { get; set; } = "Undergraduate";

    public string Duration { get; set; } = "3 Years (6 Semesters)";

    public int DurationYears { get; set; } = 3;

    public int TotalSemesters { get; set; } = 6;

    public string Department { get; set; } = string.Empty;

    public string CurriculumSummary { get; set; } = string.Empty;

    public string Color { get; set; } = "#0d9488";

    public string Status { get; set; } = "ACTIVE";

    public int TotalCreditsRequired { get; set; } = 120;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
