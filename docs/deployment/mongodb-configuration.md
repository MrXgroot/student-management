# MongoDB Configuration & Deployment Guide

This document specifies the MongoDB configuration architecture, deployment practices, and operational guidelines for **EduCore — Student & Academic Management Platform**.

---

## 1. Configuration Architecture

EduCore utilizes a centralized, typed configuration pattern to manage MongoDB connections through ASP.NET Core dependency injection.

```mermaid
graph TD
    subgraph Configuration Sources
        ENV["Environment Variables (Render / Docker)<br/>ConnectionStrings__MongoDb, DatabaseName"]
        DEV["appsettings.Development.json<br/>(Local Dev: mongodb://localhost:27017)"]
        BASE["appsettings.json<br/>(Non-secret defaults, Empty ConnectionString)"]
    end

    subgraph DI Container
        OPTIONS["IOptions&lt;MongoDbOptions&gt;"]
        CONTEXT["IMongoDbContext / MongoDbContext (Singleton)"]
        CLIENT["Single MongoClient & Connection Pool"]
        DB["IMongoDatabase"]
    end

    subgraph Domain Services
        S1["StudentService"]
        S2["AcademicService"]
        S3["TermRegistrationService"]
        S4["AttendanceService"]
        S5["FeeService"]
    end

    ENV -->|Precedence 1| OPTIONS
    DEV -->|Precedence 2 (Dev Only)| OPTIONS
    BASE -->|Precedence 3| OPTIONS

    OPTIONS --> CONTEXT
    CONTEXT --> CLIENT
    CLIENT --> DB

    CONTEXT -->|Injects IMongoDbContext| S1
    CONTEXT -->|Injects IMongoDbContext| S2
    CONTEXT -->|Injects IMongoDbContext| S3
    CONTEXT -->|Injects IMongoDbContext| S4
    CONTEXT -->|Injects IMongoDbContext| S5
```

### Key Principles
1. **Singleton Client**: A single `MongoClient` instance is shared across all services, maximizing connection pool reuse and eliminating connection exhaustion.
2. **Zero Hardcoded Secrets**: No connection strings or credentials exist in application code.
3. **Environment Driven**: Configuration follows standard ASP.NET Core precedence:
   - Environment variables (highest priority)
   - `appsettings.{Environment}.json`
   - `appsettings.json` (lowest priority)
4. **Honest Persistence**: Production requires a valid, reachable MongoDB deployment. Missing or unreachable MongoDB in Production immediately aborts startup with an informative error rather than silently defaulting to volatile in-memory storage.

---

## 2. Configuration Keys & Environment Variables

| Setting | Configuration Key | Environment Variable Key | Default Value | Notes |
| :--- | :--- | :--- | :--- | :--- |
| **Connection String** | `ConnectionStrings:MongoDb` | `ConnectionStrings__MongoDb` | `""` (Empty in base config) | In production, must be supplied via secret environment variable. |
| **Database Name** | `DatabaseName` | `DatabaseName` | `"educore_mvp"` | Target database name in MongoDB. |
| **Allow In-Memory Fallback** | `MongoDb:AllowInMemoryFallback` | `MongoDb__AllowInMemoryFallback` | `true` in Dev, `false` in Prod | Enables demo/in-memory fallback when MongoDB is unreachable. |
| **Server Selection Timeout** | `MongoDb:ServerSelectionTimeoutSeconds` | `MongoDb__ServerSelectionTimeoutSeconds` | `2` (seconds) | Maximum duration to wait for server ping during startup. |

> [!IMPORTANT]
> In Linux, Docker, and Render, nested configuration keys use the double-underscore delimiter (`__`). ASP.NET Core automatically maps `ConnectionStrings__MongoDb` to `ConnectionStrings:MongoDb`.

---

## 3. Local Development Setup

### Option A: Local MongoDB (Default for Local Development)
When running with `ASPNETCORE_ENVIRONMENT=Development`, EduCore loads [`appsettings.Development.json`](file:///d:/production/arise-craft/EduCore/backend/EduCore.Api/appsettings.Development.json):
```json
{
  "ConnectionStrings": {
    "MongoDb": "mongodb://localhost:27017"
  },
  "DatabaseName": "educore_mvp",
  "MongoDb": {
    "AllowInMemoryFallback": true,
    "ServerSelectionTimeoutSeconds": 2
  }
}
```
1. Start MongoDB locally (e.g. `docker run -d -p 27017:27017 --name mongodb mongo:8.0`).
2. Run the API:
   ```bash
   cd backend/EduCore.Api
   dotnet run
   ```
3. EduCore connects to `localhost:27017`, seeds the collections if empty, and persists all changes.

### Option B: Zero-Dependency In-Memory Demo Mode
If you do not have MongoDB installed locally, EduCore automatically detects that MongoDB is unreachable and switches to resilient in-memory storage with pre-seeded demo data (12+ fictional Indian student records, curricula, and sample transactions).
To skip the 2-second initial ping timeout, you can set `ConnectionStrings:MongoDb` to `""` in your local environment:
```bash
set ConnectionStrings__MongoDb=""
dotnet run
```

---

## 4. Configuring Secrets on Render

When deploying EduCore to Render as a Docker Web Service:

1. Open the **Render Dashboard** and navigate to your **EduCore API Web Service**.
2. Select **Environment** from the sidebar.
3. Click **Add Environment Variable**:
   - **Key:** `ConnectionStrings__MongoDb`
   - **Value:** Your secure connection string (e.g., `mongodb+srv://<username>:<password>@cluster0.abcde.mongodb.net/?retryWrites=true&w=majority`)
4. Click **Add Environment Variable**:
   - **Key:** `DatabaseName`
   - **Value:** `educore_prod` (or your chosen production database)
5. Ensure `ASPNETCORE_ENVIRONMENT` is set to `Production`.
6. Click **Save Changes**. Render will re-deploy the service with encrypted secrets injected at container startup.

> [!CAUTION]
> Never commit connection strings containing passwords to Git or Dockerfile images. Always use Render environment variables or Render Secret Files.

---

## 5. Configuring MongoDB Atlas Securely

When using **MongoDB Atlas**:

1. **Database User**: Create a dedicated database user with `readWrite` privileges restricted to the `educore_prod` database. Avoid using administrative `atlasAdmin` credentials.
2. **Network Access / IP Whitelist**:
   - Add Render's outbound IP addresses to the Atlas IP Access List (found in the Render Dashboard under **Connect** / Outbound IPs).
   - Alternatively, if using dynamic IPs, configure `0.0.0.0/0` with strong authentication credentials and TLS enforced.
3. **Connection String Format**:
   ```
   mongodb+srv://<username>:<password>@cluster0.abcde.mongodb.net/?retryWrites=true&w=majority
   ```
   Ensure special characters in the password are URL-encoded (e.g., `@` as `%40`, `:` as `%3A`).

---

## 6. Verifying Persistence Mode & Health Status

EduCore exposes an unauthenticated health check endpoint at `/health` designed for uptime monitoring and orchestrators (e.g. Render Health Checks).

### Request
```bash
curl https://<your-render-service>.onrender.com/health
```

### Response When MongoDB is Active (Healthy)
```json
{
  "status": "HEALTHY",
  "product": "EduCore MVP",
  "institution": "Vidyadeep Institute of Technology, Bengaluru",
  "timestamp": "2026-10-16T10:15:00Z",
  "persistence": "MongoDB",
  "database": "educore_prod"
}
```

### Response in Development Fallback Mode (Healthy Demo)
```json
{
  "status": "HEALTHY",
  "product": "EduCore MVP",
  "institution": "Vidyadeep Institute of Technology, Bengaluru",
  "timestamp": "2026-10-16T10:15:00Z",
  "persistence": "In-Memory Fallback",
  "database": null
}
```

### Response in Production with MongoDB Outage (Honest Reporting)
In production, if MongoDB is unreachable or misconfigured, the connection state is marked unavailable, in-memory fallback is strictly blocked, critical errors are logged, and `/health` returns **HTTP 503 Service Unavailable**:
```json
{
  "status": "UNHEALTHY",
  "product": "EduCore MVP",
  "institution": "Vidyadeep Institute of Technology, Bengaluru",
  "timestamp": "2026-10-16T10:15:00Z",
  "persistence": "Unavailable",
  "database": null
}
```

> [!NOTE]
> The health endpoint and application logs never leak passwords, user credentials, or secret connection strings.

---

## 7. Persistent MongoDB vs In-Memory Demo Storage

| Feature | Persistent MongoDB Mode | In-Memory Fallback Mode |
| :--- | :--- | :--- |
| **Target Environment** | Production, Staging, QA | Local Development, Unit Tests, Ephemeral Demos |
| **Data Retention** | Permanent across service restarts | Volatile; lost when the process terminates |
| **Concurrency / ACID** | Guaranteed by MongoDB engine | Protected by thread-safe `ConcurrentDictionary` |
| **Indexes Enforced** | Unique indexes (`admissionNumber`, `code`, compound) | Simulated in memory |
| **Startup Behavior** | Validates connection; aborts if invalid | Falls back smoothly with demo data |

---

## 8. Troubleshooting

### 1. Startup Failure: `MongoDB connection string is missing or empty in Production`
- **Cause**: The application is running with `ASPNETCORE_ENVIRONMENT=Production` but `ConnectionStrings__MongoDb` was not set.
- **Remedy**: Go to Render Dashboard -> Web Service -> Environment, and add `ConnectionStrings__MongoDb`.

### 2. Startup Failure: `Failed to connect to MongoDB database 'educore_prod' at [...]`
- **Cause**: Network timeout or cluster unreachable.
- **Checklist**:
  - Is the Atlas IP Access List configured to allow Render outbound traffic?
  - Is the username or password incorrect? (Verify URL-encoding of special characters).
  - Is the cluster paused or undergoing maintenance?

### 3. Authentication Failed: `MongoAuthenticationException: Command authenticate failed`
- **Cause**: Invalid database user credentials or wrong auth database.
- **Remedy**: Verify username and password. If the user was created in an auth database other than `admin`, append `&authSource=<authDb>` to the connection string.

---

## 9. Known Architectural Limitations

- **Runtime Reconnection Recovery**: `MongoDbContext` performs a strict ping validation during application startup. If a persistent database goes offline *after* startup has succeeded, domain operations catch driver timeouts and log warnings, but automatic re-binding from in-memory back to MongoDB without restart is outside the scope of this refactor. Orchestrators like Render will automatically restart or route to healthy instances based on the `/health` endpoint.
