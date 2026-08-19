# Expense Tracker API

![CI](https://github.com/rajeshgajra19889/ExpenseTracker/actions/workflows/ci.yml/badge.svg)



A full-featured backend REST API for tracking personal expenses, built with **ASP.NET Core (.NET 10)** and **Entity Framework Core**. Supports CRUD for expenses, categories, and budgets, JWT authentication with role-based authorization and refresh tokens, LINQ-powered reporting, and is fully containerized with Docker.

Built as a portfolio project to demonstrate practical backend .NET skills that go beyond basic CRUD: secure authentication and authorization, aggregation queries, and production-readiness concerns like versioning, caching, logging, and health checks.

---

## Features

- **CRUD APIs** for Expenses, Categories, and Budgets
- **JWT Authentication** — register/login with short-lived access tokens (15 min) and rotating refresh tokens
- **Role-Based Authorization** — `User` and `Admin` roles; admin-only endpoints for cross-user visibility
- **LINQ Reporting Endpoints** — spend by category, monthly trend, top categories, summary stats, budget vs. actual
- **Filtering, Sorting & Pagination** — filter expenses by category/date range, sort by date/amount/category, paged results
- **CSV Export** — download expenses for a date range as a CSV file
- **Soft Deletes** — deleted expenses are retained (not destroyed) via an `IsDeleted` flag and a global EF Core query filter
- **API Versioning** — endpoints are versioned (`/api/v1/...`) to support safe evolution over time
- **Response Caching** — report endpoints use output caching to reduce database load
- **Structured Logging** — request and error logging via Serilog, written to console and rolling daily log files
- **Global Exception Handling** — unhandled exceptions return a clean, consistent JSON error response
- **Health Checks** — `/health` endpoint verifies both the API and the database connection
- **Dockerized** — API and SQL Server run together via Docker Compose
- **EF Core Code-First** — migrations manage schema, relationships and business rules enforced at the database level

---

## Tech Stack

| Layer            | Technology                     |
|-------------------|----------------------------------|
| Framework        | ASP.NET Core Web API (.NET 10) |
| ORM              | Entity Framework Core          |
| Database         | SQL Server                     |
| Auth             | JWT Bearer + Refresh Tokens, Role-Based Authorization |
| Containerization | Docker, Docker Compose         |
| Logging          | Serilog                        |
| Versioning       | Asp.Versioning                 |
| Monitoring       | ASP.NET Core Health Checks     |
| API Testing      | Postman                        |

---

## Architecture

```
Controllers   →  handle HTTP requests, map to DTOs
DTOs          →  shape data in/out, never expose entities directly
Models        →  EF Core entities (User, Category, Expense, Budget)
Data          →  AppDbContext, EF Core configuration, query filters
Services      →  TokenService (JWT + refresh token generation)
Middleware    →  global exception handling
```

Entities and their relationships:

- **User** has many **Expenses** and **Budgets**, and has a **Role** (`User` or `Admin`)
- **Category** has many **Expenses** and **Budgets**
- **Expense** belongs to one User and one Category (soft-deletable)
- **Budget** belongs to one User and one Category, unique per user/category/month/year

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

A Postman collection with all requests pre-configured (including token auto-capture scripts) is available in `/postman`.

---

## API Endpoints

All endpoints are versioned under `/api/v1/`.

### Auth
| Method | Endpoint                  | Description                                          |
|--------|-----------------------------|---------------------------------------------------------|
| POST   | `/api/v1/auth/register`   | Create a new user (defaults to `User` role)          |
| POST   | `/api/v1/auth/login`      | Log in, returns an access token + refresh token       |
| POST   | `/api/v1/auth/refresh`    | Exchange a valid refresh token for a new token pair   |

Access tokens expire after 15 minutes. Refresh tokens are valid for 7 days and **rotate on every use** — each refresh call invalidates the previous refresh token and issues a new one, limiting the impact of a leaked token.

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

`sortBy` accepts `date`, `amount`, or `category`; `sortDir` accepts `asc` or `desc`. List responses are wrapped in an object with `TotalCount`, `Page`, `PageSize`, `TotalPages`, and `Data`. All expense endpoints are scoped to the logged-in user only.

### Budgets *(requires authentication)*
| Method | Endpoint                  | Description                                     |
|--------|----------------------------|--------------------------------------------------|
| GET    | `/api/v1/budgets`         | List budgets for the logged-in user             |
| POST   | `/api/v1/budgets`         | Create a monthly budget for a category          |
| DELETE | `/api/v1/budgets/{id}`    | Delete a budget                                  |

A unique database constraint prevents duplicate budgets for the same user, category, month, and year.

### Reports
| Method | Endpoint                                             | Description                                            |
|--------|---------------------------------------------------------|-----------------------------------------------------------|
| GET    | `/api/v1/reports/by-category?year=&month=`              | Total spend grouped by category                          |
| GET    | `/api/v1/reports/monthly-trend?year=`                   | Spend by month for a given year                          |
| GET    | `/api/v1/reports/top-categories?count=`                 | Highest-spending categories                               |
| GET    | `/api/v1/reports/summary`                                | Overall totals, average, current month                    |
| GET    | `/api/v1/reports/budget-status?month=&year=` *(auth)*   | Budget vs. actual spend per category, flags over-budget   |

Report endpoints are output-cached for 30 seconds to reduce database load on repeated calls.

### Admin *(requires `Admin` role)*
| Method | Endpoint                        | Description                                    |
|--------|-----------------------------------|---------------------------------------------------|
| GET    | `/api/v1/admin/all-expenses`    | View all users' expenses (not scoped to one user) |
| GET    | `/api/v1/admin/users`           | List all registered users and their roles         |

A request from an authenticated non-admin user returns `403 Forbidden`; a request with no valid token returns `401 Unauthorized`.

### Health
| Method | Endpoint   | Description                                              |
|--------|------------|--------------------------------------------------------------|
| GET    | `/health`  | Reports API and database connectivity status as JSON     |

---

## Authentication Flow

1. `POST /api/v1/auth/register` with a username, email, and password — new users default to the `User` role
2. Response includes an access `token` and a `refreshToken`
3. In Postman: **Authorization tab → Bearer Token**, set to `{{token}}`
4. When the access token expires (15 min), call `POST /api/v1/auth/refresh` with the current `refreshToken` to get a new token pair — no need to log in again
5. Admin-only endpoints require a user whose `Role` is `Admin` (promoted directly in the database — there's no self-service promotion endpoint, by design)

---

## What This Project Demonstrates

- Designing a normalized relational schema with EF Core (one-to-many relationships, cascade/restrict delete behavior, unique constraints for business rules)
- Separating entities from DTOs to control API contracts
- Writing LINQ aggregation queries (`GroupBy`, `Sum`, `OrderByDescending`, `Take`) that execute in SQL, not in memory
- Implementing database-level pagination and dynamic sorting on `IQueryable` before materializing results
- JWT authentication with short-lived access tokens and rotating refresh tokens to limit exposure from a leaked token
- Role-based authorization as a distinct concern from authentication — correctly distinguishing `401` (who are you) from `403` (you can't do that)
- Soft deletes via a global EF Core query filter, preserving data instead of destroying it
- API versioning to support evolving the contract without breaking existing consumers
- Centralized exception handling middleware and structured logging (Serilog) instead of scattered try/catch and console output
- Response caching on read-heavy report endpoints, with awareness of the resulting staleness tradeoff
- A health check endpoint that verifies real dependencies (the database), not just process liveness
- Managing secrets safely (`dotnet user-secrets` locally, environment variables in Docker) instead of committing them
- Multi-stage Docker builds and multi-container orchestration with Docker Compose

---

## Possible Future Improvements

- Unit and integration tests (xUnit, in-memory EF Core provider)
- Live cloud deployment (Azure App Service + Azure SQL)
- Refresh token reuse detection (revoke all sessions if a rotated-out token is replayed)
- Receipt image upload and storage
- Frontend client (React or Blazor)
- CI/CD pipeline to auto-deploy on push
- Budget alerts/notifications when nearing or exceeding a limit

---

## Author

**Rajesh Gajra**
Backend .NET Developer
[GitHub](https://github.com/rajeshgajra19889)
