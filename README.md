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
- Single shared application database for Identity users/roles, SaaS plans, subscriptions, monthly token usage, and agent projects.
- Seeded monthly SaaS packages:
  - `Free` - $0/month, 3 projects, 10,000 monthly tokens, `gpt-4o-mini`.
  - `Starter` - $19/month, 25 projects, 250,000 monthly tokens, `gpt-4o-mini` and `gpt-4.1-mini`.
  - `Pro` - $49/month, 100 projects, 1,000,000 monthly tokens, `gpt-4o-mini`, `gpt-4.1-mini`, and `gpt-4.1`.
- New users are automatically assigned to the Free package.
- The seeded admin is automatically assigned to the Pro package.
- Project creation enforces the current user's SaaS package quota and selected LLM model entitlement.
- Monthly token usage is tracked per user and period in `MonthlyTokenUsages`.
- Admin dashboard shows users, roles, package status, Stripe configuration, project usage, token usage, and allowed LLM models.
- Stripe Checkout for paid packages and Stripe Billing Portal for paid users.
- Stripe webhooks activate, update, mark past-due, and cancel paid subscriptions in the same application database.


## Workspace UI

The authenticated home page now uses a builder layout:

- Left side chat/build panel with SaaS package summary, token usage, model-limited project creation, and a disabled chat composer placeholder.
- Right side app preview panel with an iframe, address bar, Go button, and Reload button.
- Right side IDE placeholder panel only; no full IDE implementation is included yet. It is ready for a future Monaco, CodeMirror, or hosted editor integration.
- Saved projects remain visible below the preview/IDE area.

The preview iframe defaults to `/Plans`. Type another local route such as `/Billing`, `/Admin`, or `/Privacy` into the address bar and press Enter or Go to preview that page.

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

Paid packages use Stripe Checkout in subscription mode. Configure the Stripe price ids as monthly recurring prices. The app keeps a single database; Stripe customer/subscription ids and billing periods are stored on the existing Identity user and `UserSubscriptions` tables.

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
4. `/Billing` shows the current package, billing period, token usage, remaining monthly tokens, and allowed LLM models. It opens the Stripe Billing Portal when a Stripe customer exists.

The webhook endpoint currently handles:

- `checkout.session.completed`
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`

## Token usage and LLM model limits

Package entitlements live on `SubscriptionPlan`:

- `ProjectLimit`
- `MonthlyTokenLimit`
- `AllowedLlmModels`

The Application layer exposes `ITokenUsageService` to check model access, check remaining monthly tokens, and record usage after an LLM call.

Authenticated LLM usage can be recorded through:

```http
POST /api/usage/record
Content-Type: application/json

{
  "llmModel": "gpt-4o-mini",
  "inputTokens": 1200,
  "outputTokens": 300
}
```

The API rejects requests when the selected model is not allowed by the user's package or when the request would exceed the user's monthly token allowance.

## SOLID boundaries

- Single Responsibility: each project has one architectural responsibility.
- Open/Closed: add a new database provider, SaaS plan, or identity persistence detail without changing Domain rules.
- Liskov Substitution: repositories and services are consumed through abstractions.
- Interface Segregation: the UI depends on focused Application services instead of `DbContext`.
- Dependency Inversion: Web and Application depend on interfaces; Infrastructure supplies concrete implementations.

## Database providers

The app supports SQLite and PostgreSQL through configuration. Both providers use one database for auth, users, admins, packages, subscriptions, monthly token usage, and projects.

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

If you previously ran the older SQLite starter, delete `NanoAgent.Builder/App_Data/*.db` before first run so `EnsureCreated` can create the new Identity + SaaS + token usage schema.
