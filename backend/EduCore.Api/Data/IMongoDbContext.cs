using MongoDB.Driver;

namespace EduCore.Api.Data;

/// <summary>
/// Provides centralized access to the configured MongoDB client, database, and connection state.
/// </summary>
public interface IMongoDbContext
{
    /// <summary>
    /// The singleton MongoDB client, or null if MongoDB is unconfigured or unavailable.
    /// </summary>
    IMongoClient? Client { get; }

    /// <summary>
    /// The configured MongoDB database instance, or null if MongoDB is unconfigured or unavailable.
    /// </summary>
    IMongoDatabase? Database { get; }

    /// <summary>
    /// The configured database name.
    /// </summary>
    string DatabaseName { get; }

    /// <summary>
    /// Indicates whether the MongoDB connection is active and passed the startup ping check.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Indicates whether in-memory fallback is allowed for the active environment.
    /// </summary>
    bool AllowInMemoryFallback { get; }

    /// <summary>
    /// Retrieves a typed MongoDB collection from the active database.
    /// Throws InvalidOperationException if MongoDB is not active or connected.
    /// </summary>
    /// <typeparam name="T">The document model type.</typeparam>
    /// <param name="collectionName">The collection name.</param>
    /// <returns>The collection instance.</returns>
    IMongoCollection<T> GetCollection<T>(string collectionName);
}
