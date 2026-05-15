using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NanoAgent.Builder.Infrastructure.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AspNetRoles",
            columns: table => new
            {
                Id = table.Column<string>(nullable: false),
                Name = table.Column<string>(maxLength: 256, nullable: true),
                NormalizedName = table.Column<string>(maxLength: 256, nullable: true),
                ConcurrencyStamp = table.Column<string>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUsers",
            columns: table => new
            {
                Id = table.Column<string>(nullable: false),
                UserName = table.Column<string>(maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
                Email = table.Column<string>(maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(nullable: false),
                PasswordHash = table.Column<string>(nullable: true),
                SecurityStamp = table.Column<string>(nullable: true),
                ConcurrencyStamp = table.Column<string>(nullable: true),
                PhoneNumber = table.Column<string>(nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                TwoFactorEnabled = table.Column<bool>(nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                LockoutEnabled = table.Column<bool>(nullable: false),
                AccessFailedCount = table.Column<int>(nullable: false),
                DisplayName = table.Column<string>(maxLength: 200, nullable: true),
                StripeCustomerId = table.Column<string>(maxLength: 200, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUsers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SubscriptionPlans",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Code = table.Column<string>(maxLength: 50, nullable: false),
                Name = table.Column<string>(maxLength: 100, nullable: false),
                Description = table.Column<string>(maxLength: 500, nullable: true),
                Tier = table.Column<int>(nullable: false),
                MonthlyPrice = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(maxLength: 3, nullable: false),
                ProjectLimit = table.Column<int>(nullable: false),
                MonthlyTokenLimit = table.Column<int>(nullable: false),
                AllowedLlmModels = table.Column<string>(maxLength: 500, nullable: false),
                DisplayOrder = table.Column<int>(nullable: false),
                StripePriceId = table.Column<string>(maxLength: 200, nullable: true),
                IsActive = table.Column<bool>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AgentProjects",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OwnerUserId = table.Column<string>(maxLength: 450, nullable: false),
                Name = table.Column<string>(maxLength: 200, nullable: false),
                Description = table.Column<string>(maxLength: 1000, nullable: true),
                LlmModel = table.Column<string>(maxLength: 100, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgentProjects", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AspNetRoleClaims",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                RoleId = table.Column<string>(nullable: false),
                ClaimType = table.Column<string>(nullable: true),
                ClaimValue = table.Column<string>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserClaims",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<string>(nullable: false),
                ClaimType = table.Column<string>(nullable: true),
                ClaimValue = table.Column<string>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserLogins",
            columns: table => new
            {
                LoginProvider = table.Column<string>(maxLength: 128, nullable: false),
                ProviderKey = table.Column<string>(maxLength: 128, nullable: false),
                ProviderDisplayName = table.Column<string>(nullable: true),
                UserId = table.Column<string>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey(
                    name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserRoles",
            columns: table => new
            {
                UserId = table.Column<string>(nullable: false),
                RoleId = table.Column<string>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserTokens",
            columns: table => new
            {
                UserId = table.Column<string>(nullable: false),
                LoginProvider = table.Column<string>(maxLength: 128, nullable: false),
                Name = table.Column<string>(maxLength: 128, nullable: false),
                Value = table.Column<string>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey(
                    name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MonthlyTokenUsages",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                UserId = table.Column<string>(maxLength: 450, nullable: false),
                PeriodStartUtc = table.Column<DateTimeOffset>(nullable: false),
                PeriodEndUtc = table.Column<DateTimeOffset>(nullable: false),
                UsedTokens = table.Column<int>(nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MonthlyTokenUsages", x => x.Id);
                table.ForeignKey(
                    name: "FK_MonthlyTokenUsages_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserSubscriptions",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                UserId = table.Column<string>(maxLength: 450, nullable: false),
                SubscriptionPlanId = table.Column<Guid>(nullable: false),
                Status = table.Column<int>(nullable: false),
                StartedAtUtc = table.Column<DateTimeOffset>(nullable: false),
                EndsAtUtc = table.Column<DateTimeOffset>(nullable: true),
                StripeCustomerId = table.Column<string>(maxLength: 200, nullable: true),
                StripeSubscriptionId = table.Column<string>(maxLength: 200, nullable: true),
                StripePriceId = table.Column<string>(maxLength: 200, nullable: true),
                CurrentPeriodStartsAtUtc = table.Column<DateTimeOffset>(nullable: true),
                CurrentPeriodEndsAtUtc = table.Column<DateTimeOffset>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                    column: x => x.SubscriptionPlanId,
                    principalTable: "SubscriptionPlans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UserSubscriptions_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AgentProjects_OwnerUserId",
            table: "AgentProjects",
            column: "OwnerUserId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetRoleClaims_RoleId",
            table: "AspNetRoleClaims",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            table: "AspNetRoles",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserClaims_UserId",
            table: "AspNetUserClaims",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserLogins_UserId",
            table: "AspNetUserLogins",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserRoles_RoleId",
            table: "AspNetUserRoles",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "AspNetUsers",
            column: "NormalizedEmail");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_StripeCustomerId",
            table: "AspNetUsers",
            column: "StripeCustomerId");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            table: "AspNetUsers",
            column: "NormalizedUserName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MonthlyTokenUsages_UserId_PeriodStartUtc_PeriodEndUtc",
            table: "MonthlyTokenUsages",
            columns: new[] { "UserId", "PeriodStartUtc", "PeriodEndUtc" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SubscriptionPlans_Code",
            table: "SubscriptionPlans",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SubscriptionPlans_StripePriceId",
            table: "SubscriptionPlans",
            column: "StripePriceId");

        migrationBuilder.CreateIndex(
            name: "IX_UserSubscriptions_StripeCustomerId",
            table: "UserSubscriptions",
            column: "StripeCustomerId");

        migrationBuilder.CreateIndex(
            name: "IX_UserSubscriptions_StripeSubscriptionId",
            table: "UserSubscriptions",
            column: "StripeSubscriptionId");

        migrationBuilder.CreateIndex(
            name: "IX_UserSubscriptions_SubscriptionPlanId",
            table: "UserSubscriptions",
            column: "SubscriptionPlanId");

        migrationBuilder.CreateIndex(
            name: "IX_UserSubscriptions_UserId",
            table: "UserSubscriptions",
            column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AgentProjects");
        migrationBuilder.DropTable(name: "AspNetRoleClaims");
        migrationBuilder.DropTable(name: "AspNetUserClaims");
        migrationBuilder.DropTable(name: "AspNetUserLogins");
        migrationBuilder.DropTable(name: "AspNetUserRoles");
        migrationBuilder.DropTable(name: "AspNetUserTokens");
        migrationBuilder.DropTable(name: "MonthlyTokenUsages");
        migrationBuilder.DropTable(name: "UserSubscriptions");
        migrationBuilder.DropTable(name: "AspNetRoles");
        migrationBuilder.DropTable(name: "AspNetUsers");
        migrationBuilder.DropTable(name: "SubscriptionPlans");
    }
}
