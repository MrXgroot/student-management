using System.Text.Json;
using System.Text.Json.Serialization;
using EduCore.Api.Data;
using EduCore.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Register MongoDB options and centralized IMongoDbContext singleton
builder.Services.Configure<MongoDbOptions>(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("MongoDb");
    options.DatabaseName = builder.Configuration["DatabaseName"]
        ?? builder.Configuration["MongoDb:DatabaseName"]
        ?? MongoDbOptions.DefaultDatabaseName;

    var configuredFallback = builder.Configuration.GetValue<bool?>("MongoDb:AllowInMemoryFallback");
    options.AllowInMemoryFallback = configuredFallback ?? builder.Environment.IsDevelopment();

    var timeout = builder.Configuration.GetValue<int?>("MongoDb:ServerSelectionTimeoutSeconds");
    if (timeout.HasValue && timeout.Value > 0)
    {
        options.ServerSelectionTimeoutSeconds = timeout.Value;
    }
});

builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();

// Register Domain Services (receiving shared IMongoDbContext via DI)
builder.Services.AddSingleton<IStudentService, StudentService>();
builder.Services.AddSingleton<IAcademicService, AcademicService>();
builder.Services.AddSingleton<ITermRegistrationService, TermRegistrationService>();
builder.Services.AddSingleton<IAttendanceService, AttendanceService>();
builder.Services.AddSingleton<IFeeService, FeeService>();

// Configure CORS for frontend access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:3000","https://student-management-inky-gamma.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Enable CORS
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

// Health check endpoint reporting true persistence state without exposing secrets
app.MapGet("/health", (IMongoDbContext dbContext) =>
{
    var isMongoActive = dbContext.IsAvailable;
    var persistenceMode = isMongoActive
        ? "MongoDB"
        : (dbContext.AllowInMemoryFallback ? "In-Memory Fallback" : "Unavailable");
    var isHealthy = isMongoActive || dbContext.AllowInMemoryFallback;

    var response = new
    {
        status = isHealthy ? "HEALTHY" : "UNHEALTHY",
        product = "EduCore MVP",
        institution = "Vidyadeep Institute of Technology, Bengaluru",
        timestamp = DateTime.UtcNow,
        persistence = persistenceMode,
        database = isMongoActive ? dbContext.DatabaseName : null
    };

    return isHealthy ? Results.Ok(response) : Results.Json(response, statusCode: 503);
});

app.Run();
