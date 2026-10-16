namespace EduCore.Api.Data;

/// <summary>
/// Centralized configuration options for MongoDB persistence in EduCore.
/// </summary>
public class MongoDbOptions
{
    public const string SectionName = "MongoDb";
    public const string DefaultDatabaseName = "educore_mvp";

    /// <summary>
    /// Connection string for the MongoDB deployment.
    /// Loaded from configuration key "ConnectionStrings:MongoDb" (or env "ConnectionStrings__MongoDb").
    /// In production, this must be non-empty and point to a persistent MongoDB instance or cluster.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Name of the MongoDB database.
    /// Loaded from configuration key "DatabaseName" (or env "DatabaseName").
    /// Defaults to "educore_mvp".
    /// </summary>
    public string DatabaseName { get; set; } = DefaultDatabaseName;

    /// <summary>
    /// Controls whether the application is permitted to fall back to volatile in-memory storage
    /// when MongoDB is unreachable or unconfigured.
    /// Defaults to true in Development to allow frictionless local development and demoing.
    /// Defaults to false in Production to prevent silent data loss and false persistence claims.
    /// </summary>
    public bool AllowInMemoryFallback { get; set; } = true;

    /// <summary>
    /// Maximum time in seconds to wait for initial MongoDB server selection and connectivity ping.
    /// Defaults to 2 seconds.
    /// </summary>
    public int ServerSelectionTimeoutSeconds { get; set; } = 2;
}
