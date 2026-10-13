# OnionRiskPlatform

ASP.NET Core monolith with Onion-style layers and Dapper + PostgreSQL.

## Requirements
- .NET 9 SDK
- PostgreSQL 16+
- `psql` (to apply the schema)

## Run
Set a connection string (use a local secret store in real deployments):
```bash
export ConnectionStrings__Default='Host=localhost;Port=5432;Database=risk_platform;Username=app;Password=change-me'
psql 'host=localhost dbname=risk_platform user=app' -f database/001_schema.sql
dotnet restore
dotnet run --project src/Presentation
```
Set `PGPASSWORD` securely for `psql` if required. Never commit credentials.

Endpoints:
- `GET /health`

## Dependencies
Domain -> none; Application -> Domain; Infrastructure -> Application + Domain;
Presentation -> Application + Infrastructure (composition root only).

The SQL baseline is an initial draft. Review the assumptions in `database/README.md`
before using it with production data. Money uses `numeric`, not floating point.
