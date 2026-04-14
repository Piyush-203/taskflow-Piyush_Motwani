# TaskFlow API

A task management REST API built with **ASP.NET Core 8 (C#)** and **PostgreSQL**.

> **Note on language choice:** The assignment spec prefers Go but explicitly allows other languages.
> This implementation uses C# / .NET 8. The architecture follows a clean and minimal layered design with thin controllers and lightweight service classes. 
> This mirrors how a simple Go service would be structured, focusing on clarity

---

## Tech Stack

| Layer | Choice | Why |
|-------|--------|-----|
| Framework | ASP.NET Core 8 | Mature, performant, great DI & middleware story |
| ORM | Entity Framework Core 8 + Npgsql | Type-safe queries, migration tooling |
| Auth | JWT Bearer + BCrypt.Net | Industry standard; bcrypt cost 12 for passwords |
| Database | PostgreSQL 16 | Required by spec |
| Container | Docker (multi-stage) | Minimal runtime image ~100MB |

---

## 1. Overview

TaskFlow lets users register, log in, create projects, add tasks to those projects, and assign tasks to themselves or others. The API is RESTful, JSON-only, and fully JWT-authenticated (except `/auth` endpoints).

---

## 2. Architecture Decisions

**Layered architecture (Controller + Service)** — The application follows a simple layered design where controllers handle HTTP requests and delegate business logic to services. This keeps controllers thin while maintaining readability and avoiding over-engineering.

**`TaskItem` model name** — C# has a built-in `System.Threading.Tasks.Task`. The domain model is named `TaskItem` to avoid ambiguity; the DB table is still called `Tasks`.

**Seeding on app startup** — `DbSeeder` uses `EnsureCreated()` to initialize the schema and seed data automatically. This simplifies setup for local development.

**Status/priority as strings in DB** — Stored as `text` (enum name) rather than integers. Makes DB queries human-readable without joins to a lookup table.

---

## 3. Running Locally

**Prerequisites:** Docker and Docker Compose (nothing else needed).

```bash
git clone https://github.com/your-name/taskflow
cd taskflow

# Copy environment file
cp .env.example .env

# (Optional) Edit .env to change DB password or JWT secret

# Start everything — PostgreSQL + API
docker compose up --build

# API is now available at:
#   http://localhost:<PORT>/swagger
#   http://localhost:8080
#   http://localhost:8080/swagger  <- Example 
```

To run in the background:
```bash
docker compose up --build -d
docker compose logs -f api   # tail logs
```

To stop and remove volumes:
```bash
docker compose down -v
```

---
### Database Schema Management

The current implementation uses:

    db.Database.EnsureCreated()

to initialize the database schema during startup.

This was chosen to simplify setup in a containerized environment and ensure that the application can run with zero manual steps.

However, I acknowledge that this does not follow the assignment requirement of using explicit migrations.

## 5. Test Credentials

Seed data is inserted automatically on first startup:

```
Email:    test@example.com
Password: password123
```

A second user is also seeded:
```
Email:    jane@example.com
Password: password123
```

The seed also creates:
- 1 project: **"Website Redesign"**
- 3 tasks with statuses: `Todo`, `InProgress`, `Done`

---

## 6. API Reference

Interactive docs available at **`http://localhost:8080/swagger`** when running.

### Authentication

All endpoints except `/auth/*` require:
```
Authorization: Bearer <token>
```

#### POST `/auth/register`
```json
// Request
{ "name": "Jane Doe", "email": "jane@example.com", "password": "secret123" }

// Response 201
{ "token": "<jwt>", "user": { "id": "uuid", "name": "Jane Doe", "email": "jane@example.com" } }
```

#### POST `/auth/login`
```json
// Request
{ "email": "jane@example.com", "password": "secret123" }

// Response 200
{ "token": "<jwt>", "user": { "id": "uuid", "name": "Jane Doe", "email": "jane@example.com" } }
```

### Projects

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/projects` | List projects (owned or assigned tasks) |
| POST | `/projects` | Create project |
| GET | `/projects/:id` | Get project + tasks |
| PATCH | `/projects/:id` | Update name/description (owner only) |
| DELETE | `/projects/:id` | Delete project + tasks (owner only) |
| GET | `/projects/:id/stats` | Task counts by status and assignee *(bonus)* |

List endpoints support `?page=1&limit=20` pagination.

#### POST `/projects`
```json
// Request
{ "name": "My Project", "description": "Optional" }

// Response 201
{ "id": "uuid", "name": "My Project", "description": "Optional", "ownerId": "uuid", "createdAt": "..." }
```

#### GET `/projects/:id`
```json
// Response 200
{
  "id": "uuid", "name": "...", "description": "...", "ownerId": "uuid", "createdAt": "...",
  "tasks": [
    { "id": "uuid", "title": "...", "status": "Todo", "priority": "High", ... }
  ]
}
```

### Tasks

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/projects/:id/tasks` | List tasks (`?status=`, `?assignee=` filters) |
| POST | `/projects/:id/tasks` | Create task |
| PATCH | `/tasks/:id` | Update task fields |
| DELETE | `/tasks/:id` | Delete (owner or creator only) |

#### POST `/projects/:id/tasks`
```json
// Request
{
  "title": "Design homepage",
  "description": "Optional",
  "priority": "high",
  "assigneeId": "uuid-or-null",
  "dueDate": "2026-05-01"
}

// Response 201 — task object
```

#### PATCH `/tasks/:id`
```json
// All fields optional
{
  "title": "New title",
  "status": "in_progress",
  "priority": "low",
  "assigneeId": "uuid",
  "dueDate": "2026-06-01"
}
```

### Error Responses

```json
// 400 Validation
{ "error": "validation failed", "fields": { "email": "is required" } }

// 401 Unauthenticated
{ "error": "invalid email or password" }

// 403 Forbidden
{ "error": "forbidden" }

// 404
{ "error": "not found" }
```

A **Postman collection** is included at `TaskFlow.postman_collection.json`. Import it and run "Login" first — the token is saved automatically to a collection variable.

---

## 7. What I'd Do With More Time

- Replace `EnsureCreated()` with proper EF Core migrations
- Generate migration files using `dotnet ef migrations add`
- Apply migrations explicitly using `dotnet ef database update`
- Refresh tokens — short-lived access tokens + long-lived refresh tokens stored in the DB
- `PATCH` semantics — current implementation treats `null` as "don't update". A proper JSON Merge Patch (`application/merge-patch+json`) would allow explicitly nulling fields like `assigneeId`
- Audit log — append-only task history table showing who changed what and when

This would provide better schema versioning, traceability, and production safety.