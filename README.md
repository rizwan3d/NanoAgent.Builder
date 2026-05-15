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
- Admin dashboard shows users, roles, package status, Stripe configuration, and project usage.
- Stripe Checkout for paid packages and Stripe Billing Portal for paid users.
- Stripe webhooks activate, update, mark past-due, and cancel paid subscriptions in the same application database.

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


## Stripe billing

Paid packages use Stripe Checkout in subscription mode. The app keeps a single database; Stripe customer/subscription ids are stored on the existing Identity user and `UserSubscriptions` tables.

Configure Stripe with user secrets or environment variables instead of committing real keys:

```bash
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project NanoAgent.Builder/NanoAgent.Builder.csproj
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project NanoAgent.Builder/NanoAgent.Builder.csproj
dotnet user-secrets set "Stripe:Prices:starter" "price_..." --project NanoAgent.Builder/NanoAgent.Builder.csproj
dotnet user-secrets set "Stripe:Prices:pro" "price_..." --project NanoAgent.Builder/NanoAgent.Builder.csproj
```

Local webhook testing with the Stripe CLI:

```bash
stripe listen --forward-to https://localhost:5001/billing/stripe-webhook
```

Copy the printed `whsec_...` value into `Stripe:WebhookSecret`.

Checkout flow:

1. `/Plans` keeps Free local and redirects paid packages to Stripe Checkout.
2. Stripe redirects back to `/Billing/Success`.
3. The authoritative activation happens through `/billing/stripe-webhook`.
4. `/Billing` shows the current package and opens the Stripe Billing Portal when a Stripe customer exists.

The webhook endpoint currently handles:

- `checkout.session.completed`
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`

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
- Billing: `/Billing`
- Admin dashboard: `/Admin`

To switch provider without editing JSON:

```bash
dotnet user-secrets set "Database:Provider" "PostgreSql" --project NanoAgent.Builder/NanoAgent.Builder.csproj
dotnet user-secrets set "ConnectionStrings:PostgreSqlConnection" "Host=localhost;Port=5432;Database=nanoagent_builder;Username=postgres;Password=postgres" --project NanoAgent.Builder/NanoAgent.Builder.csproj
```

If you previously ran the older SQLite starter, delete `NanoAgent.Builder/App_Data/*.db` before first run so `EnsureCreated` can create the new Identity + SaaS schema.
