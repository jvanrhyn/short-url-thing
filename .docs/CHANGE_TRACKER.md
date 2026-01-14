# CHANGE_TRACKER

2026-01-14 | feature/api-scaffold | <initial commit> | Add API scaffolding: models, in-memory repo, URL service, API key middleware, minimal API endpoints. Note: a provisional test project was created during development but removed because current test frameworks in this environment do not yet reliably support net10; tests will be re-added or migrated when test SDK support is available.

2026-01-14 | feature/api-scaffold | add-postgres-persistence-scaffold | Add EF Core postgres scaffolding: `ShortenerDbContext`, `PostgresRepo` (implements `IRepo`), `IRepo` abstraction, DI switch to use Postgres when `ConnectionStrings:DefaultConnection` is configured, and documentation instructions for running `dotnet ef migrations` locally. Tests not added yet; will add/adjust tests once local migration and test SDK workflows for `net10` are verified. | scope: persistence scaffold | risk: medium (database migration/code paths) | breaking_change: false
