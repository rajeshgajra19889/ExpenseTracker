# Stage 1: Build using the .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Optimize layer caching
COPY *.csproj ./
RUN dotnet restore

# Copy source and publish
COPY . ./
RUN dotnet publish -c Release -o out

# Stage 2: Runtime using the lightweight .NET 10 ASP.NET image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

# .NET 10 defaults to port 8080 and runs as a non-root user
EXPOSE 8080
ENTRYPOINT ["dotnet", "ExpenseTracker.dll"]