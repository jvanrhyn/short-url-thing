# Persistence: Postgres EF Core (scaffold)

This document explains how to use the Postgres persistence scaffold included in the project.

## Overview
- `ShortenerDbContext` defines `Users` and `ShortUrls` tables.
- `PostgresRepo` implements the `IRepo` abstraction and uses EF Core to persist data.
- The app chooses Postgres at runtime when `ConnectionStrings:DefaultConnection` is set in `appsettings.json` or an environment variable.

## Local setup (development)
1. Install Postgres locally or use Docker:

   docker run --name url-shortener-db -e POSTGRES_PASSWORD=pass -e POSTGRES_USER=shortener -e POSTGRES_DB=url_shortener -p 5432:5432 -d postgres:15

2. Update `appsettings.json` or use environment variable `ConnectionStrings__DefaultConnection` to point to your database.

3. Install the EF CLI tool if not installed:

   dotnet tool install --global dotnet-ef

4. Create and apply migrations from the repository root (option A: `dotnet-ef`, option B: auto-migrate on startup)

   Option A (if you prefer `dotnet ef` locally):

   - Install EF CLI if not installed: `dotnet tool install --global dotnet-ef`
   - Create migration: `dotnet ef migrations add InitialCreate --project test-ins --startup-project test-ins`
   - Apply: `dotnet ef database update --project test-ins --startup-project test-ins`

   Option B (recommended for demo): Auto-migrate at startup — the app will call `db.Database.Migrate()` during startup when it detects a `DefaultConnection`, so running under Docker Compose (below) will automatically apply the migration included in source.

5. Run the app. If using Docker Compose, the `api` service depends on the `db` service; when the API starts it will auto-apply the included initial migration and be ready to accept requests.

## Demo: Docker Compose quickstart

1. Ensure Docker is installed and the daemon is running.

2. From the repository root run:

   docker compose up --build -d

   This will start Postgres and build/run the API. The API will auto-apply the included migration and seed a dev user with API key `dev-api-key-123` if the users table is empty.

3. Verify the API is reachable: `curl -I http://localhost:5000/` or open `http://localhost:5000/swagger`.

4. Example quick test:

   - Get current user (using seeded dev key):
     curl -H "X-Api-Key: dev-api-key-123" http://localhost:5000/users/me

   - Create a short URL:
     curl -X POST -H "X-Api-Key: dev-api-key-123" -H "Content-Type: application/json" -d '{"destination":"https://example.com"}' http://localhost:5000/urls

5. To tear down: `docker compose down -v` (this removes the database volume).


## Notes
- This is scaffold code intended for development and demonstration; production concerns (connection pooling, retry policies, migrations orchestration, secrets management) are not fully addressed here and should be implemented prior to production rollout.
