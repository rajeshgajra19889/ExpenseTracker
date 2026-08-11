# Expense Tracker API

A backend REST API for tracking personal expenses, built with **ASP.NET Core (.NET 10)** and **Entity Framework Core**. Supports full CRUD for expenses and categories, JWT-based authentication, LINQ-powered spending reports, and is fully containerized with Docker.

Built as a portfolio project to demonstrate practical backend .NET skills: EF Core data modeling, secure authentication, aggregation queries, and containerized deployment.

---

## Features

- **CRUD APIs** for Expenses, Categories, and Budgets
- **JWT Authentication** — register/login, with expenses and budgets scoped to the logged-in user
- **LINQ Reporting Endpoints** — spend by category, monthly trend, top categories, summary stats, budget vs. actual
- **Filtering, Sorting & Pagination** — filter expenses by category/date range, sort by date/amount/category, paged results
- **CSV Export** — download expenses for a date range as a CSV file
- **Soft Deletes** — deleted expenses are retained (not destroyed) via an `IsDeleted` flag and a global EF Core query filter
- **API Versioning** — endpoints are versioned (`/api/v1/...`) to support safe evolution over time
- **Response Caching** — report endpoints use output caching to reduce database load
- **Structured Logging** — request and error logging via Serilog, written to console and rolling daily log files
- **Global Exception Handling** — unhandled exceptions return a clean, consistent JSON error response
- **Dockerized** — API and SQL Server run together via Docker Compose
- **EF Core Code-First** — migrations manage schema, relationships enforced at the database level

---

## Tech Stack

| Layer          | Technology                          |
|----------------|--------------------------------------|
| Framework      | ASP.NET Core Web API (.NET 10)       |
| ORM            | Entity Framework Core                |
| Database       | SQL Server                           |
| Auth           | JWT Bearer Authentication            |
| Containerization | Docker, Docker Compose             |
| Logging        | Serilog                              |
| Versioning     | Asp.Versioning                       |
| API Testing    | Postman                              |

---

## Architecture

```
Controllers  →  handle HTTP requests, map to DTOs
DTOs         →  shape data in/out, never expose entities directly
Models       →  EF Core entities (User, Category, Expense)
Data         →  AppDbContext, EF Core configuration
Services     →  TokenService (JWT generation)
```

Entities and their relationships:

- **User** has many **Expenses**
- **Category** has many **Expenses**
- **Expense** belongs to one User and one Category

---

## Getting Started

### Option 1: Run with Docker (recommended)

Requires Docker Desktop installed and running.

```bash
git clone https://github.com/rajeshgajra19889/ExpenseTracker.git
cd ExpenseTracker
docker-compose up --build
```

This starts two containers: the API (port `8080`) and a SQL Server instance. On first run, apply migrations to the container's database:

```bash
dotnet ef database update --connection "Server=localhost,1433;Database=ExpenseTracker;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
```

The API is now available at `http://localhost:8080`.

### Option 2: Run locally

Requires .NET 10 SDK and SQL Server (LocalDB or full instance).

```bash
git clone https://github.com/rajeshgajra19889/ExpenseTracker.git
cd ExpenseTracker
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ExpenseTracker;Integrated Security=True"
dotnet user-secrets set "Jwt:Key" "your-32-character-minimum-secret-key"
dotnet user-secrets set "Jwt:Issuer" "ExpenseTrackerApi"
dotnet user-secrets set "Jwt:Audience" "ExpenseTrackerApiUsers"
dotnet ef database update
dotnet run
```

---

## API Endpoints

All endpoints are versioned under `/api/v1/`.

### Auth
| Method | Endpoint                  | Description          |
|--------|----------------------------|----------------------|
| POST   | `/api/v1/auth/register`   | Create a new user    |
| POST   | `/api/v1/auth/login`      | Log in, returns JWT   |

### Categories
| Method | Endpoint                      | Description         |
|--------|---------------------------------|----------------------|
| GET    | `/api/v1/categories`           | List all categories |
| GET    | `/api/v1/categories/{id}`      | Get one category     |
| POST   | `/api/v1/categories`           | Create a category    |
| PUT    | `/api/v1/categories/{id}`      | Update a category    |
| DELETE | `/api/v1/categories/{id}`      | Delete a category    |

### Expenses *(requires authentication)*
| Method | Endpoint                                                                          | Description                                          |
|--------|-------------------------------------------------------------------------------------|--------------------------------------------------------|
| GET    | `/api/v1/expenses?categoryId=&from=&to=&sortBy=&sortDir=&page=&pageSize=`         | List expenses — filter, sort, and paginate            |
| GET    | `/api/v1/expenses/{id}`                                                           | Get one expense                                        |
| POST   | `/api/v1/expenses`                                                                | Create an expense                                      |
| PUT    | `/api/v1/expenses/{id}`                                                           | Update an expense                                      |
| DELETE | `/api/v1/expenses/{id}`                                                           | Soft-delete an expense (flags `IsDeleted`, not removed) |
| GET    | `/api/v1/expenses/export?from=&to=`                                               | Download expenses as a CSV file                        |

`sortBy` accepts `date`, `amount`, or `category`; `sortDir` accepts `asc` or `desc`. List responses are wrapped in an object with `TotalCount`, `Page`, `PageSize`, `TotalPages`, and `Data`.

### Budgets *(requires authentication)*
| Method | Endpoint                  | Description                                     |
|--------|----------------------------|--------------------------------------------------|
| GET    | `/api/v1/budgets`         | List budgets for the logged-in user             |
| POST   | `/api/v1/budgets`         | Create a monthly budget for a category          |
| DELETE | `/api/v1/budgets/{id}`    | Delete a budget                                  |

### Reports
| Method | Endpoint                                             | Description                                            |
|--------|---------------------------------------------------------|-----------------------------------------------------------|
| GET    | `/api/v1/reports/by-category?year=&month=`              | Total spend grouped by category                          |
| GET    | `/api/v1/reports/monthly-trend?year=`                   | Spend by month for a given year                          |
| GET    | `/api/v1/reports/top-categories?count=`                 | Highest-spending categories                               |
| GET    | `/api/v1/reports/summary`                                | Overall totals, average, current month                    |
| GET    | `/api/v1/reports/budget-status?month=&year=` *(auth)*   | Budget vs. actual spend per category, flags over-budget   |

Report endpoints are output-cached for 30 seconds to reduce database load on repeated calls.

---

## Authentication Flow

1. `POST /api/v1/auth/register` with a username, email, and password
2. Response includes a JWT token
3. In Postman: **Authorization tab → Bearer Token**, paste the token
4. All `/api/v1/expenses` and `/api/v1/budgets` requests now authenticate as that user — data is automatically scoped to the logged-in user only

---

## What This Project Demonstrates

- Designing a normalized relational schema with EF Core (one-to-many relationships, cascade/restrict delete behavior, unique constraints for business rules)
- Separating entities from DTOs to control API contracts
- Writing LINQ aggregation queries (`GroupBy`, `Sum`, `OrderByDescending`, `Take`) that execute in SQL, not in memory
- Implementing database-level pagination and dynamic sorting on `IQueryable` before materializing results
- Implementing JWT authentication and scoping data access by authenticated user
- Soft deletes via a global EF Core query filter, preserving data instead of destroying it
- API versioning to support evolving the contract without breaking existing consumers
- Centralized exception handling middleware and structured logging (Serilog) instead of scattered try/catch and console output
- Response caching on read-heavy report endpoints, with awareness of the resulting staleness tradeoff
- Managing secrets safely (`dotnet user-secrets` locally, environment variables in Docker) instead of committing them
- Multi-stage Docker builds and multi-container orchestration with Docker Compose

---

## Possible Future Improvements

- Unit and integration tests (xUnit, in-memory EF Core provider)
- Live cloud deployment (Azure App Service + Azure SQL)
- Frontend client (React or Blazor)
- CI/CD pipeline to auto-deploy on push
- Budget alerts/notifications when nearing or exceeding a limit

---

## Author

**Rajesh Gajra**
Backend .NET Developer
[GitHub](https://github.com/rajeshgajra19889)
