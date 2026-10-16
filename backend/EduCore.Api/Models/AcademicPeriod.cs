using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EduCore.Api.Models;

public class AcademicPeriod
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string AcademicYear { get; set; } = "2026-2027";

    public string TermCode { get; set; } = "2026-ODD";

    public string TermName { get; set; } = "Odd Semester (Semesters 1, 3, 5)";

    [BsonIgnore]
    public string Code => TermCode;

    [BsonIgnore]
    public string Name => TermName;

    public DateTime StartDate { get; set; } = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);

    public DateTime EndDate { get; set; } = new DateTime(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc);

    public bool IsCurrent { get; set; } = true;

    public string Status { get; set; } = "ACTIVE";
}
