# Repository Guidelines

## Project Structure & Module Organization

This is an ASP.NET Core 9 monolith following **Onion Architecture** with four layers:

```
src/
├── Domain/          # Entities, aggregates, domain logic
│   ├── Assessment/  # Risk assessment entities
│   ├── Common/      # Shared domain primitives
│   ├── Import/      # Data import domain logic
│   ├── Org/         # Organization structure entities
│   ├── Report/      # Reporting domain
│   ├── Risk/        # Risk entities and rules
│   ├── Roles/       # Role definitions
│   └── Treatment/   # Risk treatment plans
├── Application/     # Use cases, services, abstractions
│   ├── Abstractions/ # Shared interfaces (e.g., IJwtTokenGenerator)
│   └── Org/         # Organization-related application logic
├── Infrastructure/  # External concerns: DB, repos, security
│   ├── Database/    # Dapper data access, migrations, connection factory
│   ├── Repositories/# Repository implementations
│   └── Security/    # JWT token generation, password hashing
└── Presentation/    # ASP.NET Core API (controllers, middleware, DI composition root)
```

**Dependency direction**: `Domain` → `Application` → `Infrastructure` → `Presentation`. No layer may reference its outer layers.

## Build, Test, and Development Commands

| Command | Description |
|---|---|
| `dotnet restore` | Restore NuGet packages for the solution |
| `dotnet build` | Build all four projects |
| `dotnet run --project src/Presentation` | Start the API locally |
| `dotnet build -c Release` | Build in release mode |

**Prerequisites**: `.NET 9 SDK`, **PostgreSQL 16+**, and a configured connection string.

**Quick start**:
```powershell
# Set connection string (use user secrets or env vars in real deployments)
$env:ConnectionStrings__Default = "Host=localhost;Port=5432;Database=risk_platform;Username=app;Password=change-me"

# Apply the database schema
psql 'host=localhost dbname=risk_platform user=app' -f src\Infrastructure\Database\Migrations\001_InitialSchema.sql

# Run the API
dotnet run --project src/Presentation
```

**Docker**: A multi-stage `Dockerfile` is in `src/Presentation/`. Build with:
```powershell
docker build -f src/Presentation/Dockerfile -t risk-platform .
```

## Coding Style & Naming Conventions

Enforced via `Directory.Build.props`:

- **Nullable context**: `enable` — all reference types are non-nullable by default
- **Implicit usings**: `enable` — global usings are auto-generated
- **Language version**: `latest`

**Naming patterns**:
- Controllers: `{Resource}Controller.cs` (e.g., `AssetsController.cs`, `AuthController.cs`)
- Domain entities: POCOs in their respective domain folders
- Application services: interface + implementation in `Application/`
- Repositories: `I{Entity}Repository` / `{Entity}Repository` in `Infrastructure/`

**API conventions**: RESTful controllers with JWT Bearer authentication. Swagger UI is available at `/swagger` in Development.

## Testing Guidelines

No test projects currently exist in the repository. When adding tests:

- Use **xUnit** or **NUnit** with a project named `tests/OnionRiskPlatform.Tests`
- Follow the same Onion layering — test Application services and Domain logic, not controllers directly
- Mock infrastructure dependencies via the abstractions in `Application/Abstractions/`

## Commit & Pull Request Guidelines

The project uses **short, imperative** commit messages:

```
Impl Roles CRUD
Impl User CRUD
fix DELETE у директорий
Update pre.yml
```

Guidelines:
- Keep subjects under ~50 characters
- Use imperative mood: `Impl X`, `fix Y`, `Add Z`
- Russian is acceptable for commit messages (mixed language is common)
- Prefix with scope when helpful: `Auth: impl login`, `Org: fix cascade delete`

## Security & Configuration Tips

- **Never commit credentials**. The `appsettings.json` contains a placeholder JWT secret — always override via environment variables or user secrets in development.
- JWT `SecretKey` must be **at least 32 bytes** (enforced at startup).
- Passwords are hashed using `Microsoft.AspNetCore.Identity.PasswordHasher`.
- The dev endpoint `/_dev/hash/{password}` (available only in Development) can be used for quick password hashing — remove or restrict in production.
- HTTPS redirection is enforced in production via `app.UseHttpsRedirection()`.

## Architecture Overview

```
┌─────────────────────────────────────────────────┐
│              Presentation (API)                  │
│   Controllers → DTOs → Application Services     │
├─────────────────────────────────────────────────┤
│              Application (Use Cases)             │
│   Commands/Queries → Domain Entities → Results  │
├─────────────────────────────────────────────────┤
│            Infrastructure (Adapters)             │
│   Dapper + PostgreSQL, Repos, JWT, Migrations   │
├─────────────────────────────────────────────────┤
│                 Domain (Core)                    │
│   Entities, Value Objects, Domain Rules         │
└─────────────────────────────────────────────────┘
```

Money values use `numeric` type in PostgreSQL — never floating point.
