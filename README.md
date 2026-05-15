# NanoAgent.Builder

This solution is structured around project-level Clean Architecture and SOLID principles.

## Projects

- `NanoAgent.Builder.Domain` - enterprise/domain rules, entities, and domain exceptions. It has no dependency on web, EF Core, or infrastructure.
- `NanoAgent.Builder.Application` - use cases, DTOs, and abstractions. It depends on Domain and defines interfaces that Infrastructure implements.
- `NanoAgent.Builder.Infrastructure` - EF Core database implementation, repositories, unit of work, and database-provider selection.
- `NanoAgent.Builder` - ASP.NET Core Razor Pages UI and composition root.

## SOLID boundaries

- Single Responsibility: each project has one architectural responsibility.
- Open/Closed: add a new database provider by extending Infrastructure without changing Domain or Application rules.
- Liskov Substitution: repositories and services are consumed through abstractions.
- Interface Segregation: the UI depends on focused Application services instead of DbContext.
- Dependency Inversion: Web and Application depend on interfaces; Infrastructure supplies concrete implementations.

## Database providers

The app supports SQLite and PostgreSQL through configuration.

Default SQLite configuration:

```json
"Database": {
  "Provider": "Sqlite",
  "EnsureCreated": true
},
"ConnectionStrings": {
  "SqliteConnection": "Data Source=App_Data/nanoagent-builder.db"
}
```

PostgreSQL configuration:

```json
"Database": {
  "Provider": "PostgreSql",
  "EnsureCreated": true
},
"ConnectionStrings": {
  "PostgreSqlConnection": "Host=localhost;Port=5432;Database=nanoagent_builder;Username=postgres;Password=postgres"
}
```

`Database:EnsureCreated` is enabled for a lightweight starter workflow. For production, set it to `false` and use EF Core migrations.

## Run

```bash
dotnet restore
dotnet run --project NanoAgent.Builder/NanoAgent.Builder.csproj
```

To switch provider without editing JSON:

```bash
dotnet user-secrets set "Database:Provider" "PostgreSql" --project NanoAgent.Builder/NanoAgent.Builder.csproj
dotnet user-secrets set "ConnectionStrings:PostgreSqlConnection" "Host=localhost;Port=5432;Database=nanoagent_builder;Username=postgres;Password=postgres" --project NanoAgent.Builder/NanoAgent.Builder.csproj
```
