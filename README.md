# PowerFit API

> A backend API that generates **personalized workout and nutrition plans with AI**, checks every plan against **safety rules** (injuries, allergies, age, BMI), and lets an **admin review and edit** the result.

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-ASP.NET%20Core-239120?logo=csharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-EF%20Core-CC2927?logo=microsoftsqlserver&logoColor=white)
![Tests](https://img.shields.io/badge/tests-xUnit-5E5E5E)

<!-- TODO: add a Swagger screenshot (docs/swagger.png) and a Live Demo link if you deploy it -->
<!-- ![Swagger UI](./docs/swagger.png) -->

## Overview

Generic fitness plans ignore the person they are given to. PowerFit takes a user's profile (goal, experience, equipment, injuries, dietary restrictions, allergens) and asks an LLM to build a weekly exercise plan and a nutrition plan **using only the exercises and foods stored in the database**. The model's answer is never trusted: every ID, range, and calorie total is validated before anything is saved, and unsafe combinations (for example a knee exercise for a user with a knee injury) are rejected.

## Features

- **AI plan generation** through the Anthropic Messages API, with per-user and global daily limits
- **Safety engine**: injury-aware exercise filtering, dietary flags (vegetarian, gluten-free, etc.), allergen exclusion, minimum age, BMI and calorie floors
- **Admin review workflow**: admins can edit exercise and nutrition items, and manage the exercise and food catalogs
- **Plan lifecycle**: Draft → Active → Archived, with a database-enforced rule of **one active plan per user**
- **JWT authentication** with role-based policies (User / Admin) and first-admin seeding
- **Bilingual body-area matching**: Arabic and English terms map to the same area (`الركبة` = `knee`)
- **Consistent error responses** through a single exception-handling middleware (400 / 401 / 404 / 409 / 429 / 504)

## Architecture

Three-tier architecture in three separate projects. The API layer depends only on the Business layer and never references a data-access type.

```
Powerfit.API  (presentation)  ──►  Powerfit.Business  (logic)  ──►  DataAccess  (data)
Controllers, Middleware            Services, DTOs, Validators       EF Core, Repositories,
JWT, Rate limiting, CORS           AI, Security, Mapping            Unit of Work, Entities
```

### How a plan is generated

```
POST /api/plans/generate
   │
   ├─ 1. Validate the request (age ≥ 16, goal rules, BMI)
   ├─ 2. Load exercises and foods, then filter out anything unsafe for this user
   │      (injuries, dietary restrictions, allergens)
   ├─ 3. Sample the catalogs evenly by muscle group / food category to keep the prompt small
   ├─ 4. Call the Anthropic Messages API and extract the JSON answer
   ├─ 5. AiPlanValidator: check every ID, range, and total against the catalogs that were sent
   └─ 6. Save the plan in one transaction (or return a clear error, saving nothing)
```

## Tech Stack

| Area | Technology |
|---|---|
| Framework | ASP.NET Core Web API on .NET 10, C# |
| Database | SQL Server, Entity Framework Core 10 |
| Auth | JWT Bearer, role-based authorization policies |
| AI | Anthropic Messages API (model configurable in `appsettings.json`) |
| Docs | Swagger / OpenAPI (Swashbuckle) |
| Testing | xUnit |

## API Overview

| Area | Endpoints | Access |
|---|---|---|
| Auth | `POST /api/auth/register`, `/login`, `/change-password` | Public (rate limited) / Authenticated |
| Profile | `GET`, `PUT /api/profile` | User |
| Plans | `POST /api/plans/generate`, `GET /api/plans`, `GET /api/plans/active`, `GET /api/plans/{id}`, `POST /api/plans/{id}/activate`, `POST /api/plans/{id}/archive`, `DELETE /api/plans/{id}` | User |
| Catalog | `GET /api/exercises`, `/muscle-groups`, `/equipment`, `GET /api/foods`, `/categories` | Authenticated |
| Admin: catalogs | CRUD + `PATCH .../{id}/active` on `/api/admin/exercises` and `/api/admin/foods` | Admin |
| Admin: plans | list, activate, archive, delete, edit exercise/nutrition items and targets under `/api/admin/plans` | Admin |
| Admin: accounts | `POST /api/admin/admins` | Admin |

Full interactive docs are available in Swagger (enabled in Development).

## Security

- **Password hashing:** PBKDF2-HMAC-SHA512 with 210,000 iterations, a per-password salt, constant-time comparison, and automatic rehash when parameters change
- **Brute-force protection:** 5 failed logins within 15 minutes lock the account for 15 minutes (`429` with `Retry-After`)
- **Timing-attack resistance:** login always performs a password check, using a dummy hash when the account does not exist
- **Rate limiting** on auth and AI-generation endpoints, plus daily generation caps per user and globally to control AI cost
- **Secrets** are kept in user-secrets or environment variables and never committed
- **CORS** is deny-by-default outside Development, and forwarded headers trust only configured proxies

## Technical Decisions & Challenges

**Never trust the model's output.**
LLMs can invent IDs or return impossible numbers. `AiPlanValidator` checks every exercise ID, food ID, range, and calorie total against the exact catalogs that were sent in the prompt, so invalid data never reaches the database constraints.

**Exactly one active plan per user, even under concurrency.**
A filtered unique index (`UX_FitnessPlan_OneActivePerUser`) enforces the rule in the database. Activation archives the user's other plans first and runs inside a transaction, so two simultaneous requests cannot both succeed.

**Two admins editing the same plan.**
A `rowversion` column on `FitnessPlan` gives optimistic concurrency. Concurrent edits return `409 Conflict` instead of silently overwriting each other.

**Database errors without leaking SQL details.**
`DbUpdateException` is translated into clean `409` responses (duplicate value, record in use, record modified by someone else) inside the data layer, so the API layer stays unaware of SQL Server.

**Safety first.**
Plans require a birth date (no default age), a minimum age of 16, Fat Loss goals from 18 with BMI ≥ 18.5, and a 1200 kcal floor shared between AI-generated and admin-edited plans. Admin edits that conflict with a user's injuries or dietary restrictions are rejected unless `overrideSafetyCheck: true` is sent deliberately. Every plan carries a medical disclaimer field.

**Keeping prompts small and cost predictable.**
Catalogs are sampled evenly across muscle groups and food categories (capped at 200 items each by default), and the AI request timeout is configurable.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (Visual Studio 2026 if you use Visual Studio; the target framework can be changed in `Directory.Build.props`)
- SQL Server (LocalDB or full)
- An Anthropic API key

### Setup

```bash
git clone https://github.com/omarmadadha7-crypto/PowerFit.git
cd PowerFit
```

1. Create a database named `PowerFit` and run the table script, then run `Database/001_safety_and_integrity.sql`.
   <!-- TODO: add Database/000_schema.sql to the repo and mention it here -->
2. Adjust `ConnectionStrings:DefaultConnection` in `src/Powerfit.API/appsettings.json` if needed.
3. Store secrets with user-secrets:

```bash
cd src/Powerfit.API
dotnet user-secrets set "Jwt:Key" "<random string, 32+ characters>"
dotnet user-secrets set "Ai:ApiKey" "<your Anthropic API key>"
dotnet user-secrets set "AdminSeed:Email" "admin@example.com"
dotnet user-secrets set "AdminSeed:Password" "<strong password>"
```

4. Run:

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Powerfit.API
```

Swagger UI: `https://localhost:7080/swagger` (Development). On first start, an admin account is created from `AdminSeed` if none exists. Remove the seed password afterwards.

### Configuration

| Key | Purpose | Default |
|---|---|---|
| `Jwt:Key`, `Jwt:ExpiryMinutes` | Token signing key and lifetime | none / 60 |
| `Ai:ApiKey`, `Ai:Model` | AI provider credentials and model | none / see `appsettings.json` |
| `Ai:MaxGenerationsPerUserPerDay` | Per-user generation cap (rolling 24h) | 10 |
| `Ai:MaxGenerationsPerDay` | Global generation cap | 500 |
| `Cors:AllowedOrigins` | Allowed front-end origins | none |
| `ForwardedHeaders:TrustedProxies` | Proxies trusted for real client IP | none |
| `Swagger:Enabled` | Enable Swagger outside Development | false |

## Testing

```bash
dotnet test
```

xUnit unit tests cover the pure business logic: body-area matching, exercise safety against injuries, dietary flags and allergens, BMI and calorie rules, `AiPlanValidator`, input validators, password hashing, and login lockout.

## Project Structure

```
PowerFit/
├── Database/                  SQL scripts (constraints, indexes, rowversion)
├── docs/CHANGES.md            Change log with the reasoning behind each decision
├── src/
│   ├── Powerfit.API/          Controllers, middleware, auth, rate limiting, Swagger
│   ├── Powerfit.Business/     Services, AI generation and validation, DTOs, validators, security
│   └── DataAccess/            EF Core context, entities, repositories, unit of work
└── tests/Powerfit.Tests/      xUnit tests
```

## Roadmap

- [ ] Integration tests against SQL Server
- [ ] Email verification and JWT revocation
- [ ] Retry logic and token-usage logging for AI calls
- [ ] Replace free-text injuries and dietary restrictions with a fixed list chosen from the UI
- [ ] Front-end client

## Author

**Omar Madadha** · [GitHub](https://github.com/omarmadadha7-crypto) · [LinkedIn](#) · your.email@example.com
