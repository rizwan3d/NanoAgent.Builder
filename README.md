# NanoAgent.Builder

This solution is structured around project-level Clean Architecture, SOLID principles, and a single-database SaaS starter model.

## Projects

- `NanoAgent.Builder.Domain` - enterprise/domain rules, entities, and domain exceptions. It has no dependency on web, EF Core, or infrastructure.
- `NanoAgent.Builder.Application` - use cases, DTOs, and abstractions. It depends on Domain and defines interfaces that Infrastructure implements.
- `NanoAgent.Builder.Infrastructure` - EF Core database implementation, ASP.NET Core Identity persistence, repositories, SaaS seed data, unit of work, and database-provider selection.
- `NanoAgent.Builder` - ASP.NET Core Razor Pages UI and composition root.

## Added SaaS/auth features

- ASP.NET Core Identity authentication for users and admins.
- Role-based admin access with `Admin` and `User` roles.
- Single shared application database for Identity users/roles, SaaS plans, subscriptions, and agent projects.
- Seeded SaaS packages:
  - `Free` - $0/month, 3 projects.
  - `Starter` - $19/month, 25 projects.
  - `Pro` - $49/month, 100 projects.
- New users are automatically assigned to the Free package.
- The seeded admin is automatically assigned to the Pro package.
- Project creation enforces the current user's SaaS package quota.
- Admin dashboard shows users, roles, package status, and project usage.

## Default admin account

Configured in `appsettings.json` under `SeedAdmin`:

```json
"SeedAdmin": {
  "Email": "admin@nanoagent.local",
  "Password": "Admin#12345",
  "DisplayName": "System Admin"
}
```

Change this password before using the project anywhere beyond local development.

## SOLID boundaries

- Single Responsibility: each project has one architectural responsibility.
- Open/Closed: add a new database provider, SaaS plan, or identity persistence detail without changing Domain rules.
- Liskov Substitution: repositories and services are consumed through abstractions.
- Interface Segregation: the UI depends on focused Application services instead of `DbContext`.
- Dependency Inversion: Web and Application depend on interfaces; Infrastructure supplies concrete implementations.

## Database providers

The app supports SQLite and PostgreSQL through configuration. Both providers use one database for auth, users, admins, packages, subscriptions, and projects.

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

Then open the app and use:

- User signup: `/Account/Register`
- Login: `/Account/Login`
- Packages: `/Plans`
- Admin dashboard: `/Admin`

To switch provider without editing JSON:

```bash
dotnet user-secrets set "Database:Provider" "PostgreSql" --project NanoAgent.Builder/NanoAgent.Builder.csproj
dotnet user-secrets set "ConnectionStrings:PostgreSqlConnection" "Host=localhost;Port=5432;Database=nanoagent_builder;Username=postgres;Password=postgres" --project NanoAgent.Builder/NanoAgent.Builder.csproj
```

If you previously ran the older SQLite starter, delete `NanoAgent.Builder/App_Data/*.db` before first run so `EnsureCreated` can create the new Identity + SaaS schema.
