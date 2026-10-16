using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EduCore.Api.Data;

/// <summary>
/// Centralized implementation of IMongoDbContext that owns the singleton MongoClient,
/// database configuration, startup connectivity validation, and safe host logging.
/// </summary>
public class MongoDbContext : IMongoDbContext
{
    private readonly ILogger<MongoDbContext> _logger;
    private readonly IMongoClient? _client;
    private readonly IMongoDatabase? _database;
    private readonly string _databaseName;
    private readonly bool _isAvailable;
    private readonly bool _allowInMemoryFallback;

    public IMongoClient? Client => _client;
    public IMongoDatabase? Database => _database;
    public string DatabaseName => _databaseName;
    public bool IsAvailable => _isAvailable;
    public bool AllowInMemoryFallback => _allowInMemoryFallback;

    public MongoDbContext(IOptions<MongoDbOptions> optionsAccessor, ILogger<MongoDbContext>? logger = null, IHostEnvironment? env = null)
        : this(optionsAccessor.Value, logger ?? NullLogger<MongoDbContext>.Instance, env)
    {
    }

    public static MongoDbContext FromConfiguration(IConfiguration configuration, ILogger<MongoDbContext>? logger = null, IHostEnvironment? env = null)
    {
        return new MongoDbContext(BuildOptionsFromConfiguration(configuration, env), logger ?? NullLogger<MongoDbContext>.Instance, env);
    }

    public MongoDbContext(MongoDbOptions options, ILogger<MongoDbContext>? logger = null, IHostEnvironment? env = null)
    {
        _logger = logger ?? NullLogger<MongoDbContext>.Instance;
        _databaseName = string.IsNullOrWhiteSpace(options.DatabaseName)
            ? MongoDbOptions.DefaultDatabaseName
            : options.DatabaseName.Trim();
        _allowInMemoryFallback = options.AllowInMemoryFallback;

        var connectionString = options.ConnectionString?.Trim();

        // 1. Validate presence of connection string
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _isAvailable = false;
            _client = null;
            _database = null;

            if (!_allowInMemoryFallback)
            {
                _logger.LogCritical(
                    "MongoDB connection string is missing or empty in Production. " +
                    "Please configure 'ConnectionStrings__MongoDb' (or 'ConnectionStrings:MongoDb') with a valid MongoDB connection string. " +
                    "In-memory fallback is disabled in Production to prevent silent data loss.");
                return;
            }

            _logger.LogInformation("No MongoDB connection string configured. Development/Demo mode active: using resilient In-Memory storage.");
            return;
        }

        // 2. Attempt connection and connectivity validation
        var sanitizedHost = SanitizeConnectionStringForLogging(connectionString);
        try
        {
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(Math.Max(1, options.ServerSelectionTimeoutSeconds));

            var client = new MongoClient(settings);
            var database = client.GetDatabase(_databaseName);

            // Execute an explicit ping command to verify server reachability and credentials
            var pingTimeout = TimeSpan.FromSeconds(settings.ServerSelectionTimeout.TotalSeconds + 0.5);
            var pingTask = database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");

            if (!pingTask.Wait(pingTimeout))
            {
                throw new TimeoutException($"MongoDB ping timed out after {pingTimeout.TotalSeconds} seconds.");
            }

            _client = client;
            _database = database;
            _isAvailable = true;

            _logger.LogInformation("Successfully connected to MongoDB at {Host}, Database: '{DatabaseName}'", sanitizedHost, _databaseName);
        }
        catch (Exception ex)
        {
            _isAvailable = false;
            _client = null;
            _database = null;

            if (!_allowInMemoryFallback)
            {
                _logger.LogCritical(
                    ex,
                    "Failed to connect to MongoDB database '{DatabaseName}' at {Host}. " +
                    "In-memory fallback is disabled in Production to prevent silent data loss.",
                    _databaseName,
                    sanitizedHost);
                return;
            }

            _logger.LogWarning(ex, "MongoDB is unavailable at {Host}. Development/Demo mode active: defaulting to resilient In-Memory storage.", sanitizedHost);
        }
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        if (_database == null || !_isAvailable)
        {
            throw new InvalidOperationException(
                $"Cannot access MongoDB collection '{collectionName}' because MongoDB is not connected or active.");
        }

        return _database.GetCollection<T>(collectionName);
    }

    private static MongoDbOptions BuildOptionsFromConfiguration(IConfiguration configuration, IHostEnvironment? env)
    {
        var isDevelopment = env?.IsDevelopment() ??
            string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);

        var options = new MongoDbOptions
        {
            ConnectionString = configuration.GetConnectionString("MongoDb"),
            DatabaseName = configuration["DatabaseName"] ?? configuration["MongoDb:DatabaseName"] ?? MongoDbOptions.DefaultDatabaseName
        };

        var configuredFallback = configuration.GetValue<bool?>("MongoDb:AllowInMemoryFallback");
        options.AllowInMemoryFallback = configuredFallback ?? isDevelopment;

        var timeout = configuration.GetValue<int?>("MongoDb:ServerSelectionTimeoutSeconds");
        if (timeout.HasValue && timeout.Value > 0)
        {
            options.ServerSelectionTimeoutSeconds = timeout.Value;
        }

        return options;
    }

    /// <summary>
    /// Safely extracts host and port details without ever logging credentials, passwords, or tokens.
    /// </summary>
    private static string SanitizeConnectionStringForLogging(string connectionString)
    {
        try
        {
            var url = new MongoUrl(connectionString);
            var servers = url.Servers.Select(s => $"{s.Host}:{s.Port}").ToList();
            if (servers.Count > 0)
            {
                return $"[{string.Join(", ", servers)}]";
            }
        }
        catch
        {
            // If parsing fails, never log the raw string because it could contain passwords
        }

        return "[sanitized-connection-string]";
    }
}
