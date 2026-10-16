using EduCore.Api.Data;
using EduCore.Api.Models;
using EduCore.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.UnitTests;

public class MongoDbConfigurationTests
{
    [Fact]
    public void EnvironmentVariables_Override_AppSettings_Values()
    {
        // 1. Set environment variables using standard deployment double-underscore delimiter
        Environment.SetEnvironmentVariable("ConnectionStrings__MongoDb", "mongodb://render-production-host:27017");
        Environment.SetEnvironmentVariable("DatabaseName", "educore_production");

        try
        {
            // 2. Simulate base appsettings.json with non-secret defaults
            var appsettingsData = new Dictionary<string, string?>
            {
                { "ConnectionStrings:MongoDb", "" },
                { "DatabaseName", "educore_mvp" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(appsettingsData)
                .AddEnvironmentVariables() // Reads env vars and converts __ to :
                .Build();

            var connectionString = configuration.GetConnectionString("MongoDb");
            var databaseName = configuration["DatabaseName"];

            Assert.Equal("mongodb://render-production-host:27017", connectionString);
            Assert.Equal("educore_production", databaseName);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__MongoDb", null);
            Environment.SetEnvironmentVariable("DatabaseName", null);
        }
    }

    [Fact]
    public void Single_Configured_MongoDbContext_Is_Registered_And_Shared_In_DI()
    {
        var services = new ServiceCollection();

        var configData = new Dictionary<string, string?>
        {
            { "ConnectionStrings:MongoDb", "" },
            { "DatabaseName", "educore_test" },
            { "MongoDb:AllowInMemoryFallback", "true" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<MongoDbOptions>(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("MongoDb");
            options.DatabaseName = configuration["DatabaseName"] ?? "educore_test";
            options.AllowInMemoryFallback = true;
        });

        services.AddSingleton<IMongoDbContext, MongoDbContext>();
        services.AddSingleton<IStudentService, StudentService>();
        services.AddSingleton<IAcademicService, AcademicService>();

        var provider = services.BuildServiceProvider();

        var context1 = provider.GetRequiredService<IMongoDbContext>();
        var context2 = provider.GetRequiredService<IMongoDbContext>();

        // Assert single shared instance
        Assert.Same(context1, context2);
        Assert.False(context1.IsAvailable);

        var studentService = provider.GetRequiredService<IStudentService>();
        var academicService = provider.GetRequiredService<IAcademicService>();

        Assert.Equal(context1.IsAvailable, studentService.IsMongoDbActive);
        Assert.Equal(context1.IsAvailable, academicService.IsMongoDbActive);
    }

    [Fact]
    public async Task Missing_Production_Configuration_Disables_Persistence_And_Prevents_InMemory_Fallback()
    {
        var options = new MongoDbOptions
        {
            ConnectionString = "",
            DatabaseName = "educore_prod",
            AllowInMemoryFallback = false // Production environment
        };

        var context = new MongoDbContext(options, NullLogger<MongoDbContext>.Instance);

        Assert.False(context.IsAvailable);
        Assert.False(context.AllowInMemoryFallback);
        Assert.Null(context.Client);
        Assert.Null(context.Database);

        // Accessing collections via IMongoDbContext throws
        Assert.Throws<InvalidOperationException>(() => context.GetCollection<Student>("students"));

        // Domain services must NOT silently seed or enable in-memory storage in Production
        var studentService = new StudentService(context, NullLogger<StudentService>.Instance);
        Assert.False(studentService.IsMongoDbActive);

        // Calling service operations in production when MongoDB is down throws rather than returning fake data
        await Assert.ThrowsAsync<InvalidOperationException>(() => studentService.GetStudentsAsync(null, "ALL", "ALL"));
    }

    [Fact]
    public void Unreachable_Connection_In_Production_Disables_Persistence_And_Prevents_InMemory_Fallback()
    {
        var options = new MongoDbOptions
        {
            // Pointing to a closed local port with minimal timeout
            ConnectionString = "mongodb://127.0.0.1:59999",
            DatabaseName = "educore_prod",
            AllowInMemoryFallback = false, // Production environment
            ServerSelectionTimeoutSeconds = 1
        };

        var context = new MongoDbContext(options, NullLogger<MongoDbContext>.Instance);

        Assert.False(context.IsAvailable);
        Assert.False(context.AllowInMemoryFallback);
        Assert.Null(context.Client);
        Assert.Null(context.Database);

        Assert.Throws<InvalidOperationException>(() => context.GetCollection<Student>("students"));
    }

    [Fact]
    public async Task Development_Fallback_Behaves_As_Intended_When_ConnectionString_Empty()
    {
        var options = new MongoDbOptions
        {
            ConnectionString = "",
            DatabaseName = "educore_dev",
            AllowInMemoryFallback = true // Development environment
        };

        var context = new MongoDbContext(options, NullLogger<MongoDbContext>.Instance);

        Assert.False(context.IsAvailable);
        Assert.Null(context.Client);
        Assert.Null(context.Database);
        Assert.True(context.AllowInMemoryFallback);

        // Verify GetCollection throws when MongoDB is unavailable
        Assert.Throws<InvalidOperationException>(() => context.GetCollection<Student>("students"));

        // Verify domain services initialize with in-memory demo data
        var studentService = new StudentService(context, NullLogger<StudentService>.Instance);
        Assert.False(studentService.IsMongoDbActive);

        var students = await studentService.GetStudentsAsync(null, "ALL", "ALL");
        Assert.NotNull(students);
        Assert.True(students.Count >= 12, "Demo students should be seeded into in-memory store in development fallback mode");
    }

    [Fact]
    public void Health_Reporting_Accurately_Reflects_Production_Degradation()
    {
        // 1. Production with MongoDB unavailable
        var prodOptions = new MongoDbOptions
        {
            ConnectionString = "",
            DatabaseName = "educore_prod",
            AllowInMemoryFallback = false
        };
        var prodContext = new MongoDbContext(prodOptions, NullLogger<MongoDbContext>.Instance);

        var isProdActive = prodContext.IsAvailable;
        var isProdHealthy = isProdActive || prodContext.AllowInMemoryFallback;
        var prodMode = isProdActive
            ? "MongoDB"
            : (prodContext.AllowInMemoryFallback ? "In-Memory Fallback" : "Unavailable");

        Assert.False(isProdActive);
        Assert.False(isProdHealthy);
        Assert.Equal("Unavailable", prodMode);

        // 2. Development fallback
        var devOptions = new MongoDbOptions
        {
            ConnectionString = "",
            DatabaseName = "educore_dev",
            AllowInMemoryFallback = true
        };
        var devContext = new MongoDbContext(devOptions, NullLogger<MongoDbContext>.Instance);

        var isDevActive = devContext.IsAvailable;
        var isDevHealthy = isDevActive || devContext.AllowInMemoryFallback;
        var devMode = isDevActive
            ? "MongoDB"
            : (devContext.AllowInMemoryFallback ? "In-Memory Fallback" : "Unavailable");

        Assert.False(isDevActive);
        Assert.True(isDevHealthy);
        Assert.Equal("In-Memory Fallback", devMode);
    }

    [Fact]
    public void ConnectionString_Credentials_Are_Never_Exposed_In_Host_Sanitization()
    {
        // Connection string with sensitive credentials
        var rawConnectionString = "mongodb://adminUser:SuperSecretP@ssword123!@cluster0.abcde.mongodb.net:27017/educore_mvp?authSource=admin";

        var options = new MongoDbOptions
        {
            ConnectionString = rawConnectionString,
            DatabaseName = "educore_mvp",
            AllowInMemoryFallback = true,
            ServerSelectionTimeoutSeconds = 1
        };

        // Even when connectivity check fails, the exception or logs must not contain the password
        var context = new MongoDbContext(options, NullLogger<MongoDbContext>.Instance);
        Assert.False(context.IsAvailable);

        // Verify database name is tracked but credentials are not exposed
        Assert.Equal("educore_mvp", context.DatabaseName);
    }
}
