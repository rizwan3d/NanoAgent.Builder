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
    private readonly IProjectWorkspaceFileSystem _workspaceFileSystem;
    private readonly IProjectWorkspaceSetupQueue _setupQueue;
    private readonly IUnitOfWork _unitOfWork;

    public AgentProjectService(
        ICurrentUserContext currentUser,
        IAgentProjectRepository projects,
        IProjectQuotaService quotaService,
        ITokenUsageService tokenUsageService,
        IProjectStorageRepository projectStorage,
        IProjectWorkspaceFileSystem workspaceFileSystem,
        IProjectWorkspaceSetupQueue setupQueue,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _projects = projects;
        _quotaService = quotaService;
        _tokenUsageService = tokenUsageService;
        _projectStorage = projectStorage;
        _workspaceFileSystem = workspaceFileSystem;
        _setupQueue = setupQueue;
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

        var files = await _projectStorage.ListFilesAsync(project.Id, cancellationToken);
        await _workspaceFileSystem.EnsureProjectWorkspaceAsync(project, files, cancellationToken);

        await _projectStorage.AddArtifactAsync(
            new GeneratedArtifact(
                project.Id,
                null,
                "Workspace setup queued",
                "workspace-setup",
                null,
                "Workspace setup has been queued and will run shortly."),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _setupQueue.QueueAsync(project.Id, cancellationToken);

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
        var description = string.IsNullOrWhiteSpace(project.Description)
            ? "A focused product workspace ready for planning, editing, and previewing."
            : project.Description;
        var packageName = BuildPackageName(project.Name);
        var titleLiteral = ToTypeScriptString(project.Name);
        var descriptionLiteral = ToTypeScriptString(description);

        await _projectStorage.AddFileAsync(
            new ProjectFile(
                project.Id,
                "README.md",
                "markdown",
                $"""
                # {project.Name}

                {description}

                ## Starter stack

                - App Router
                - Component-based pages
                - TypeScript
                - Local workspace files

                ## Local commands

                ```bash
                npm install
                npm run build
                npm run dev
                ```

                The workspace setup runs install and build automatically after the project is created. Review the setup artifact for command output.
                """),
            cancellationToken);

        await _projectStorage.AddFileAsync(
            new ProjectFile(
                project.Id,
                "package.json",
                "json",
                $$"""
                {
                  "name": "{{packageName}}",
                  "private": true,
                  "version": "0.1.0",
                  "scripts": {
                    "dev": "next dev",
                    "build": "next build",
                    "start": "next start"
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

                // This file is generated by the framework and should not be edited manually.
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
                $$"""
                import type { Metadata } from "next";
                import "./globals.css";

                export const metadata: Metadata = {
                  title: {{titleLiteral}},
                  description: {{descriptionLiteral}}
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
                $$"""
                const projectName = {{titleLiteral}};
                const projectDescription = {{descriptionLiteral}};

                const sections = [
                  "Shape the landing page",
                  "Add product flows",
                  "Preview every change"
                ];

                export default function HomePage() {
                  return (
                    <main className="page-shell">
                      <section className="hero-card">
                        <p className="eyebrow">Project workspace</p>
                        <h1>{projectName}</h1>
                        <p className="lead">{projectDescription}</p>
                        <div className="section-grid">
                          {sections.map((section) => (
                            <article className="section-card" key={section}>
                              <span>0{sections.indexOf(section) + 1}</span>
                              <h2>{section}</h2>
                              <p>Use the editor to turn this starter into a focused product experience.</p>
                            </article>
                          ))}
                        </div>
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
                  --surface: rgba(255, 255, 255, 0.9);
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
                  width: min(980px, 100%);
                  padding: clamp(28px, 6vw, 56px);
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
                  font-size: clamp(2.6rem, 7vw, 5rem);
                  line-height: 0.95;
                }

                .lead {
                  margin: 20px 0 0;
                  max-width: 48rem;
                  color: var(--muted);
                  font-size: 1.1rem;
                  line-height: 1.7;
                }

                .section-grid {
                  display: grid;
                  grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));
                  gap: 16px;
                  margin-top: 36px;
                }

                .section-card {
                  padding: 20px;
                  border: 1px solid var(--border);
                  border-radius: 20px;
                  background: rgba(255, 255, 255, 0.7);
                }

                .section-card span {
                  color: var(--accent);
                  font-size: 0.8rem;
                  font-weight: 700;
                }

                .section-card h2 {
                  margin: 12px 0 8px;
                  font-size: 1.1rem;
                }

                .section-card p {
                  margin: 0;
                  color: var(--muted);
                  line-height: 1.6;
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
                  "projectName": {{ToJsonString(project.Name)}},
                  "description": {{ToJsonString(description)}},
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
                "Initial workspace files",
                "workspace-note",
                "README.md",
                "A project-specific starter was created and queued for setup."),
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

    private static string BuildPackageName(string projectName)
    {
        var characters = projectName
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();
        var slug = string.Join('-', new string(characters).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? "workspace-app" : slug;
    }

    private static string ToTypeScriptString(string value) => ToJsonString(value);

    private static string ToJsonString(string value) =>
        "\"" + value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal) + "\"";

    private static AgentProjectDto MapToDto(AgentProject project) =>
        new(project.Id, project.OwnerUserId, project.Name, project.Description, project.LlmModel, project.CreatedAtUtc);
}