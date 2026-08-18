# Stage 1: Build using the .NET 10 SDK
FROM ://microsoft.com AS build-env
WORKDIR /app

# Optimize layer caching
COPY *.csproj ./
RUN dotnet restore

# Copy source and publish
COPY . ./
RUN dotnet publish -c Release -o out

# Stage 2: Runtime using the lightweight .NET 10 ASP.NET image
FROM ://microsoft.com
WORKDIR /app
COPY --from=build-env /app/out .

# .NET 10 defaults to port 8080 and runs as a non-root user
EXPOSE 8080
ENTRYPOINT ["dotnet", "ExpenseTracker.dll"]
