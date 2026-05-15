using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NanoAgent.Builder.Infrastructure.Migrations;

public partial class AddProjectStorage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProjectFiles",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                ProjectId = table.Column<Guid>(nullable: false),
                Path = table.Column<string>(maxLength: 500, nullable: false),
                Language = table.Column<string>(maxLength: 100, nullable: true),
                Content = table.Column<string>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProjectFiles", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProjectFiles_AgentProjects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "AgentProjects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProjectMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                ProjectId = table.Column<Guid>(nullable: false),
                Role = table.Column<string>(maxLength: 50, nullable: false),
                Content = table.Column<string>(nullable: false),
                LlmModel = table.Column<string>(maxLength: 100, nullable: false),
                InputTokens = table.Column<int>(nullable: false),
                OutputTokens = table.Column<int>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProjectMessages", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProjectMessages_AgentProjects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "AgentProjects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProjectRuns",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                ProjectId = table.Column<Guid>(nullable: false),
                Status = table.Column<string>(maxLength: 50, nullable: false),
                RequestedModel = table.Column<string>(maxLength: 100, nullable: false),
                Prompt = table.Column<string>(maxLength: 8000, nullable: true),
                InputTokens = table.Column<int>(nullable: false),
                OutputTokens = table.Column<int>(nullable: false),
                StartedAtUtc = table.Column<DateTimeOffset>(nullable: false),
                CompletedAtUtc = table.Column<DateTimeOffset>(nullable: true),
                ErrorMessage = table.Column<string>(maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProjectRuns", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProjectRuns_AgentProjects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "AgentProjects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "GeneratedArtifacts",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                ProjectId = table.Column<Guid>(nullable: false),
                ProjectRunId = table.Column<Guid>(nullable: true),
                Name = table.Column<string>(maxLength: 200, nullable: false),
                ArtifactType = table.Column<string>(maxLength: 100, nullable: false),
                Path = table.Column<string>(maxLength: 500, nullable: true),
                Content = table.Column<string>(nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GeneratedArtifacts", x => x.Id);
                table.ForeignKey(
                    name: "FK_GeneratedArtifacts_AgentProjects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "AgentProjects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_GeneratedArtifacts_ProjectRuns_ProjectRunId",
                    column: x => x.ProjectRunId,
                    principalTable: "ProjectRuns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_GeneratedArtifacts_ProjectId_CreatedAtUtc",
            table: "GeneratedArtifacts",
            columns: new[] { "ProjectId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_GeneratedArtifacts_ProjectRunId",
            table: "GeneratedArtifacts",
            column: "ProjectRunId");

        migrationBuilder.CreateIndex(
            name: "IX_ProjectFiles_ProjectId_Path",
            table: "ProjectFiles",
            columns: new[] { "ProjectId", "Path" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProjectMessages_ProjectId_CreatedAtUtc",
            table: "ProjectMessages",
            columns: new[] { "ProjectId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_ProjectRuns_ProjectId_StartedAtUtc",
            table: "ProjectRuns",
            columns: new[] { "ProjectId", "StartedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_ProjectRuns_Status",
            table: "ProjectRuns",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "GeneratedArtifacts");
        migrationBuilder.DropTable(name: "ProjectFiles");
        migrationBuilder.DropTable(name: "ProjectMessages");
        migrationBuilder.DropTable(name: "ProjectRuns");
    }
}
