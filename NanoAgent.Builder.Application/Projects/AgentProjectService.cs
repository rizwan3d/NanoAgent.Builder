using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Domain.Common;
using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Application.Projects;

internal sealed class AgentProjectService : IAgentProjectService
{
    private readonly ICurrentUserContext _currentUser;
    private readonly IAgentProjectRepository _projects;
    private readonly IProjectQuotaService _quotaService;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly IProjectStorageRepository _projectStorage;
    private readonly IUnitOfWork _unitOfWork;

    public AgentProjectService(
        ICurrentUserContext currentUser,
        IAgentProjectRepository projects,
        IProjectQuotaService quotaService,
        ITokenUsageService tokenUsageService,
        IProjectStorageRepository projectStorage,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _projects = projects;
        _quotaService = quotaService;
        _tokenUsageService = tokenUsageService;
        _projectStorage = projectStorage;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AgentProjectDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        RequireSignedInUser();

        var projects = _currentUser.IsAdmin
            ? await _projects.ListAllAsync(cancellationToken)
            : await _projects.ListForOwnerAsync(_currentUser.UserId!, cancellationToken);

        return projects.Select(MapToDto).ToList();
    }

    public async Task<AgentProjectDto> CreateAsync(CreateAgentProjectRequest request, CancellationToken cancellationToken = default)
    {
        var userId = RequireSignedInUser();

        await _quotaService.EnsureCanCreateProjectAsync(userId, cancellationToken);
        await _tokenUsageService.EnsureModelAllowedAsync(userId, request.LlmModel, cancellationToken);

        var project = new AgentProject(userId, request.Name, request.Description, request.LlmModel);

        await _projects.AddAsync(project, cancellationToken);
        await SeedProjectStorageAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(project);
    }

    public async Task<AgentProjectDto> RenameAsync(RenameAgentProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = await GetOwnedOrAdminProjectAsync(request.ProjectId, cancellationToken);

        project.Rename(request.Name);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(project);
    }

    public async Task DeleteAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await GetOwnedOrAdminProjectAsync(projectId, cancellationToken);

        _projects.Remove(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }


    private async Task SeedProjectStorageAsync(AgentProject project, CancellationToken cancellationToken)
    {
        await _projectStorage.AddFileAsync(
            new ProjectFile(
                project.Id,
                "README.md",
                "markdown",
                $"""
                # {project.Name}

                This project starts with a minimal Next.js app.

                ## Starter stack

                - Next.js with the App Router
                - React 18
                - TypeScript

                ## Key files

                - `app/layout.tsx`
                - `app/page.tsx`
                - `app/globals.css`
                - `package.json`
                - `tsconfig.json`
                """),
            cancellationToken);

        await _projectStorage.AddFileAsync(
            new ProjectFile(
                project.Id,
                "package.json",
                "json",
                """
                {
                  "name": "nanoagent-next-app",
                  "private": true,
                  "version": "0.1.0",
                  "scripts": {
                    "dev": "next dev",
                    "build": "next build",
                    "start": "next start",
                    "lint": "next lint"
                  },
                  "dependencies": {
                    "next": "14.2.15",
                    "react": "18.3.1",
                    "react-dom": "18.3.1"
                  },
                  "devDependencies": {
                    "@types/node": "22.10.2",
                    "@types/react": "18.3.12",
                    "@types/react-dom": "18.3.1",
                    "eslint": "8.57.1",
                    "eslint-config-next": "14.2.15",
                    "typescript": "5.6.3"
                  }
                }
                """),
            cancellationToken);

        await _projectStorage.AddFileAsync(
            new ProjectFile(
                project.Id,
                "tsconfig.json",
                "json",
                """
                {
                  "compilerOptions": {
                    "target": "ES2017",
                    "lib": ["dom", "dom.iterable", "esnext"],
                    "allowJs": false,
                    "skipLibCheck": true,
                    "strict": true,
                    "noEmit": true,
                    "esModuleInterop": true,
                    "module": "esnext",
                    "moduleResolution": "bundler",
                    "resolveJsonModule": true,
                    "isolatedModules": true,
                    "jsx": "preserve",
                    "incremental": true,
                    "plugins": [
                      {
                        "name": "next"
                      }
                    ]
                  },
                  "include": ["next-env.d.ts", "**/*.ts", "**/*.tsx", ".next/types/**/*.ts"],
                  "exclude": ["node_modules"]
                }
                """),
            cancellationToken);

        await _projectStorage.AddFileAsync(
            new ProjectFile(
                project.Id,
                "next-env.d.ts",
                "typescript",
                """
                /// <reference types="next" />
                /// <reference types="next/image-types/global" />

                // This file is auto-generated by Next.js and should not be edited manually.
                """),
            cancellationToken);

        await _projectStorage.AddFileAsync(
            new ProjectFile(
                project.Id,
                "next.config.ts",
                "typescript",
                """
                import type { NextConfig } from "next";

                const nextConfig: NextConfig = {};

                export default nextConfig;
                """),
            cancellationToken);

        await _projectStorage.AddFileAsync(
            new ProjectFile(
                project.Id,
                "app/layout.tsx",
                "typescript",
                """
                import type { Metadata } from "next";
                import "./globals.css";

                export const metadata: Metadata = {
                  title: "NanoAgent Next App",
                  description: "A Next.js starter generated for NanoAgent Builder."
                };

                export default function RootLayout({
                  children
                }: Readonly<{
                  children: React.ReactNode;
                }>) {
                  return (
                    <html lang="en">
                      <body>{children}</body>
                    </html>
                  );
                }
                """),
            cancellationToken);

        await _projectStorage.AddFileAsync(
            new ProjectFile(
                project.Id,
                "app/page.tsx",
                "typescript",
                """
                export default function HomePage() {
                  return (
                    <main className="page-shell">
                      <section className="hero-card">
                        <p className="eyebrow">NanoAgent Builder</p>
                        <h1>Next.js project created successfully.</h1>
                        <p className="lead">
                          This starter uses the App Router and is ready for the workspace chat to evolve.
                        </p>
                      </section>
                    </main>
                  );
                }
                """),
            cancellationToken);

        await _projectStorage.AddFileAsync(
            new ProjectFile(
                project.Id,
                "app/globals.css",
                "css",
                """
                :root {
                  color-scheme: light;
                  --background: #f4efe7;
                  --surface: rgba(255, 255, 255, 0.88);
                  --text: #1f1b18;
                  --muted: #6f6258;
                  --accent: #c96f3b;
                  --accent-deep: #8f4a26;
                  --border: rgba(31, 27, 24, 0.12);
                }

                * {
                  box-sizing: border-box;
                }

                html,
                body {
                  margin: 0;
                  min-height: 100%;
                }

                body {
                  font-family: Georgia, "Times New Roman", serif;
                  background:
                    radial-gradient(circle at top, rgba(201, 111, 59, 0.18), transparent 32%),
                    linear-gradient(180deg, #fcf8f2 0%, var(--background) 100%);
                  color: var(--text);
                }

                .page-shell {
                  min-height: 100vh;
                  display: grid;
                  place-items: center;
                  padding: 32px;
                }

                .hero-card {
                  width: min(720px, 100%);
                  padding: 48px;
                  border: 1px solid var(--border);
                  border-radius: 28px;
                  background: var(--surface);
                  box-shadow: 0 24px 80px rgba(64, 35, 19, 0.12);
                }

                .eyebrow {
                  margin: 0 0 16px;
                  color: var(--accent-deep);
                  font-size: 0.8rem;
                  font-weight: 700;
                  letter-spacing: 0.18em;
                  text-transform: uppercase;
                }

                h1 {
                  margin: 0;
                  font-size: clamp(2.5rem, 7vw, 4.75rem);
                  line-height: 0.95;
                }

                .lead {
                  margin: 20px 0 0;
                  max-width: 40rem;
                  color: var(--muted);
                  font-size: 1.1rem;
                  line-height: 1.7;
                }
                """),
            cancellationToken);

        await _projectStorage.AddFileAsync(
            new ProjectFile(
                project.Id,
                "agent.config.json",
                "json",
                $$"""
                {
                  "projectId": "{{project.Id}}",
                  "llmModel": "{{project.LlmModel}}",
                  "storage": {
                    "files": "ProjectFiles",
                    "messages": "ProjectMessages",
                    "runs": "ProjectRuns",
                    "artifacts": "GeneratedArtifacts"
                  }
                }
                """),
            cancellationToken);

        await _projectStorage.AddArtifactAsync(
            new GeneratedArtifact(
                project.Id,
                null,
                "Initial workspace artifact",
                "workspace-note",
                "README.md",
                "Starter Next.js project storage was created."),
            cancellationToken);
    }

    private async Task<AgentProject> GetOwnedOrAdminProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (projectId == Guid.Empty)
        {
            throw new DomainException("Project id is required.");
        }

        var userId = RequireSignedInUser();
        var project = await _projects.GetByIdAsync(projectId, cancellationToken);

        if (project is null)
        {
            throw new DomainException("The selected project was not found.");
        }

        if (!_currentUser.IsAdmin && !string.Equals(project.OwnerUserId, userId, StringComparison.Ordinal))
        {
            throw new DomainException("You do not have permission to manage this project.");
        }

        return project;
    }

    private string RequireSignedInUser()
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            throw new DomainException("You must sign in before managing projects.");
        }

        return _currentUser.UserId;
    }

    private static AgentProjectDto MapToDto(AgentProject project) =>
        new(project.Id, project.OwnerUserId, project.Name, project.Description, project.LlmModel, project.CreatedAtUtc);
}
