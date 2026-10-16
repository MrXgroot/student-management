using System.Text.Json;
using System.Text.Json.Serialization;
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

// Register Domain Services (with MongoDB and resilient in-memory fallback)
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
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:3000")
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

// Health check endpoint
app.MapGet("/health", (IStudentService studentService) => Results.Ok(new
{
    status = "HEALTHY",
    product = "EduCore MVP",
    institution = "Vidyadeep Institute of Technology, Bengaluru",
    timestamp = DateTime.UtcNow,
    persistence = studentService.IsMongoDbActive ? "MongoDB" : "In-Memory Fallback"
}));

app.Run();
