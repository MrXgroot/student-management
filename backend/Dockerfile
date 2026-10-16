# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project definition and restore dependencies
COPY ["EduCore.Api/EduCore.Api.csproj", "EduCore.Api/"]
RUN dotnet restore "EduCore.Api/EduCore.Api.csproj"

# Copy remaining source code and publish in Release configuration
COPY EduCore.Api/ EduCore.Api/
WORKDIR "/src/EduCore.Api"
RUN dotnet publish "EduCore.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Switch to standard non-root user defined in ASP.NET Core base image
USER $APP_UID

# Copy published application artifacts
COPY --from=build /app/publish .

# Configure ASP.NET Core to bind to 0.0.0.0 and port 10000 (standard for Render web service deployment)
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV ASPNETCORE_HTTP_PORTS=10000

# Expose container port
EXPOSE 10000

# Execute the application
ENTRYPOINT ["dotnet", "EduCore.Api.dll"]
