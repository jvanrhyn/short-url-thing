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

4. Create and apply migrations from the repository root:

   dotnet ef migrations add InitialCreate --project test-ins --startup-project test-ins
   dotnet ef database update --project test-ins --startup-project test-ins

5. Run the app. It will use Postgres for persistence.

## Notes
- This is scaffold code intended for development and demonstration; production concerns (connection pooling, retry policies, migrations orchestration, secrets management) are not fully addressed here and should be implemented prior to production rollout.
