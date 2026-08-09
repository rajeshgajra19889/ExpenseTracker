# Expense Tracker API

A backend REST API for tracking personal expenses, built with **ASP.NET Core (.NET 10)** and **Entity Framework Core**. Supports full CRUD for expenses and categories, JWT-based authentication, LINQ-powered spending reports, and is fully containerized with Docker.

Built as a portfolio project to demonstrate practical backend .NET skills: EF Core data modeling, secure authentication, aggregation queries, and containerized deployment.

---

## Features

- **CRUD APIs** for Expenses and Categories
- **JWT Authentication** — register/login, with expenses scoped to the logged-in user
- **LINQ Reporting Endpoints** — spend by category, monthly trend, top categories, summary stats
- **Filtering & Querying** — filter expenses by category and date range
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

### Auth
| Method | Endpoint              | Description          |
|--------|------------------------|----------------------|
| POST   | `/api/auth/register`  | Create a new user    |
| POST   | `/api/auth/login`     | Log in, returns JWT   |

### Categories
| Method | Endpoint                  | Description         |
|--------|----------------------------|----------------------|
| GET    | `/api/categories`          | List all categories |
| GET    | `/api/categories/{id}`     | Get one category     |
| POST   | `/api/categories`          | Create a category    |
| PUT    | `/api/categories/{id}`     | Update a category    |
| DELETE | `/api/categories/{id}`     | Delete a category    |

### Expenses *(requires authentication)*
| Method | Endpoint                                              | Description                      |
|--------|--------------------------------------------------------|-----------------------------------|
| GET    | `/api/expenses?categoryId=&from=&to=`                  | List expenses, with filters      |
| GET    | `/api/expenses/{id}`                                   | Get one expense                  |
| POST   | `/api/expenses`                                        | Create an expense                |
| PUT    | `/api/expenses/{id}`                                   | Update an expense                |
| DELETE | `/api/expenses/{id}`                                   | Delete an expense                |

### Reports
| Method | Endpoint                                | Description                             |
|--------|-------------------------------------------|-------------------------------------------|
| GET    | `/api/reports/by-category?year=&month=`  | Total spend grouped by category         |
| GET    | `/api/reports/monthly-trend?year=`       | Spend by month for a given year         |
| GET    | `/api/reports/top-categories?count=`     | Highest-spending categories             |
| GET    | `/api/reports/summary`                    | Overall totals, average, current month |

---

## Authentication Flow

1. `POST /api/auth/register` with a username, email, and password
2. Response includes a JWT token
3. In Postman: **Authorization tab → Bearer Token**, paste the token
4. All `/api/expenses` requests now authenticate as that user — data is automatically scoped to the logged-in user only

---

## What This Project Demonstrates

- Designing a normalized relational schema with EF Core (one-to-many relationships, cascade/restrict delete behavior)
- Separating entities from DTOs to control API contracts
- Writing LINQ aggregation queries (`GroupBy`, `Sum`, `OrderByDescending`, `Take`) that execute in SQL, not in memory
- Implementing JWT authentication and scoping data access by authenticated user
- Managing secrets safely (`dotnet user-secrets` locally, environment variables in Docker) instead of committing them
- Multi-stage Docker builds and multi-container orchestration with Docker Compose

---

## Possible Future Improvements

- Budget entity with monthly limits and alerts
- Pagination on list endpoints
- Unit and integration tests
- Frontend client (React or Blazor)
- CI/CD pipeline to auto-deploy on push

---

## Author

**Rajesh Gajra**
Backend .NET Developer
[GitHub](https://github.com/rajeshgajra19889)
