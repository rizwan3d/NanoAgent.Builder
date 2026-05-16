# NanoAgent Builder

NanoAgent Builder is a SaaS-ready AI app builder for creating, managing, and evolving software projects from a clean web workspace.

It gives each user a dashboard, project workspace, code editor, app preview, usage tracking, plan limits, and Stripe-powered billing foundation. New projects start with a ready-to-edit Next.js starter so customers can move from idea to working app structure faster.

---

## What is NanoAgent Builder?

NanoAgent Builder helps users create app projects, chat with an AI workspace assistant, review generated output, edit project files, and manage project usage from one browser-based product.

Instead of starting from an empty folder, users can create a project and immediately open a workspace with:

- A project dashboard
- A chat-style assistant panel
- A file browser
- A Monaco code editor
- A live preview area
- Run history
- Token usage tracking
- Plan and billing management

NanoAgent Builder is designed for founders, developers, agencies, indie builders, and SaaS teams who want a structured AI-assisted app-building environment.

---

## Key Features

### Project Dashboard

Create and manage projects from a simple dashboard.

Users can:

- Create new projects
- Open projects in the workspace
- Rename projects
- Delete projects
- View project limits
- View current plan
- View monthly token usage

---

### AI Workspace

Each project opens into a focused builder workspace with a chat panel, preview tab, code tab, file tree, editor, and run history.

The workspace includes:

- Project selector
- Chat composer
- LLM model selector
- Remaining token display
- Generated messages
- Run history
- Project files
- Saveable code editor
- Preview iframe

---

### Starter Next.js Project

Every new project is seeded with a minimal Next.js app structure, including:

- `README.md`
- `package.json`
- `tsconfig.json`
- `next-env.d.ts`
- `next.config.ts`
- `app/layout.tsx`
- `app/page.tsx`
- `app/globals.css`
- `agent.config.json`

The generated starter uses:

- Next.js App Router
- React
- TypeScript
- CSS starter styling
- Local workspace files

---

### Code Editing

NanoAgent Builder includes a Monaco-powered code editor experience so users can inspect and update generated project files directly from the browser.

Users can:

- Browse project files
- Select a file
- View file language metadata
- Edit content
- Save changes
- Keep workspace files synced to disk

---

### App Preview

The workspace includes a browser-style preview panel with:

- Reload button
- Address bar
- Go button
- Preview iframe

This gives users a familiar way to review their app while working inside the builder.

---

### Usage and Token Tracking

NanoAgent Builder includes built-in monthly token usage tracking.

Customers can see:

- Current plan
- Monthly token limit
- Used tokens
- Remaining tokens
- Active usage period
- Allowed LLM models

The system enforces model access and token limits based on the user’s active SaaS plan.

---

### SaaS Plans

NanoAgent Builder includes a SaaS package system with support for:

- Free plans
- Paid plans
- Project limits
- Monthly token limits
- Allowed LLM models
- Plan upgrades
- Stripe Checkout
- Stripe customer portal
- Stripe webhooks

This makes it suitable as a foundation for a commercial AI builder product.

---

### Billing

Customers can select a plan and, when payment is required, continue through Stripe Checkout.

Billing features include:

- Subscription checkout
- Stripe customer creation
- Stripe subscription metadata
- Billing portal access
- Webhook endpoint for Stripe events
- Current subscription display
- Usage display

---

### Authentication

NanoAgent Builder includes account registration, login, roles, and protected pages.

New users start on the free plan automatically after registration.

Protected areas include:

- Dashboard
- Workspace
- Billing
- Admin dashboard

---

### Admin Dashboard

Administrators can view SaaS and customer activity from the admin dashboard.

The admin dashboard shows:

- Total users
- Total projects
- Active subscriptions
- Package details
- User roles
- User plans
- Project counts
- Monthly token usage
- Allowed LLM models

---

## Who is NanoAgent Builder for?

NanoAgent Builder is useful for:

- SaaS founders building an AI app generator
- Agencies creating apps for clients
- Developers prototyping ideas quickly
- Product teams experimenting with AI-assisted development
- Builders who want project, billing, and workspace features in one product

---

## Example Use Cases

### Build a SaaS app prototype

Create a project, describe what you want, review generated workspace notes, and evolve the Next.js starter files.

### Offer AI app generation to customers

Use NanoAgent Builder as the foundation for a customer-facing AI builder platform with plans, limits, and Stripe billing.

### Manage multiple AI-generated projects

Keep each project organized with its own files, messages, runs, artifacts, and workspace folder.

### Track customer usage

Use built-in token usage and plan limits to control how many projects and tokens each customer can use.

---

## Tech Stack

NanoAgent Builder is built with:

- .NET 10
- ASP.NET Core Razor Pages
- Entity Framework Core
- ASP.NET Core Identity
- PostgreSQL
- SQLite
- Stripe
- Monaco Editor
- Next.js starter workspaces
- Docker support

---

## Architecture

The solution is organized into four main projects:

```text
NanoAgent.Builder.Domain
NanoAgent.Builder.Application
NanoAgent.Builder.Infrastructure
NanoAgent.Builder
```

### Domain

Contains core business entities and domain rules for projects, workspace storage, SaaS plans, subscriptions, token usage, and generated artifacts.

### Application

Contains product services and use cases, including:

* Project management
* Workspace operations
* SaaS subscription logic
* Token usage tracking
* Project quota checks
* Admin dashboard data

### Infrastructure

Contains implementation details, including:

* Entity Framework Core database context
* Repositories
* ASP.NET Core Identity integration
* Stripe payment integration
* Workspace file system support
* Database configuration

### Web App

Contains the Razor Pages customer experience:

* Dashboard
* Workspace
* Plans
* Billing
* Account pages
* Admin dashboard
* Stripe webhook endpoint
* Usage API endpoint

---

## Getting Started

### Prerequisites

Install:

* .NET 10 SDK
* PostgreSQL or SQLite
* Node.js and npm for running generated Next.js workspaces
* Stripe account for paid billing flows

---

## Configuration

NanoAgent Builder supports PostgreSQL and SQLite.

Default database configuration is stored in `appsettings.json`.

Example:

```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ApplyMigrations": true
  },
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=App_Data/nanoagent-builder.db",
    "PostgreSqlConnection": "Host=localhost;Port=5432;Database=nanoagent_builder;Username=postgres;Password=root"
  },
  "Stripe": {
    "SecretKey": "",
    "WebhookSecret": "",
    "Prices": {
      "starter": "",
      "pro": ""
    }
  },
  "ProjectWorkspaces": {
    "RootPath": "App_Data/Workspaces"
  }
}
```

For local development, update the database provider and connection string as needed.

---

## Run Locally

Clone the repository:

```bash
git clone https://github.com/rizwan3d/NanoAgent.Builder.git
cd NanoAgent.Builder
```

Restore packages:

```bash
dotnet restore
```

Run the web app:

```bash
dotnet run --project NanoAgent.Builder/NanoAgent.Builder.csproj
```

Open the local URL shown in the terminal.

---

## Running a Generated Workspace

When a project is created, NanoAgent Builder stores a Next.js starter workspace.

From the workspace root shown in the app, run:

```bash
npm install
npm run dev
```

Then use the preview panel or browser to view the running app.

---

## Docker

The repository includes a Dockerfile for containerized builds.

Build the image:

```bash
docker build -f NanoAgent.Builder/Dockerfile -t nanoagent-builder .
```

Run the container:

```bash
docker run -p 8080:8080 nanoagent-builder
```

---

## Stripe Setup

To enable paid subscriptions:

1. Create products and recurring prices in Stripe.
2. Add your Stripe secret key to configuration.
3. Add Stripe price IDs for the paid plans.
4. Configure the Stripe webhook secret.
5. Point your Stripe webhook to:

```text
/billing/stripe-webhook
```

Paid plans will redirect customers to Stripe Checkout when selected.

---

## Customer Flow

1. Customer creates an account.
2. Customer starts on the free plan.
3. Customer creates a project.
4. NanoAgent Builder creates a starter Next.js workspace.
5. Customer opens the workspace.
6. Customer chats with NanoAgent.
7. NanoAgent records runs, messages, artifacts, and usage.
8. Customer edits files in the code editor.
9. Customer previews the project.
10. Customer upgrades plan when more capacity is needed.

---

## Admin Flow

Admins can open the admin dashboard to review:

* Users
* Roles
* Plans
* Subscriptions
* Projects
* Token usage
* LLM model entitlements
* Stripe plan configuration status

---

## Roadmap Ideas

Planned or suggested future improvements:

* Real LLM provider integration
* Background project generation jobs
* GitHub export
* One-click deployment
* Live preview server orchestration
* Team workspaces
* File diff view
* Prompt history
* App templates
* Custom model providers
* Usage analytics
* Admin plan editor
* Customer invoices and receipts page

---

## Why NanoAgent Builder?

NanoAgent Builder gives you a strong starting point for a commercial AI app builder.

It already includes the product foundation many AI tools need:

* Accounts
* Projects
* Workspaces
* Code editing
* Usage metering
* SaaS plans
* Billing hooks
* Admin visibility
* Database persistence
* Starter app generation

Use it as a base to launch your own AI-powered builder, coding assistant, app generator, or developer platform.

---


## Support

For questions, issues, or custom development, open an issue in the repository.

