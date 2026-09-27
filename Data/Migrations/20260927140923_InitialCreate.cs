using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PromptForge.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EncryptedKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Avatar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptOptimizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptimizedContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetModel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UseCase = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OutputFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HealthScoreOriginal = table.Column<int>(type: "int", nullable: true),
                    HealthScoreOptimized = table.Column<int>(type: "int", nullable: true),
                    TokensOriginal = table.Column<int>(type: "int", nullable: true),
                    TokensOptimized = table.Column<int>(type: "int", nullable: true),
                    ClarifyingQuestions = table.Column<bool>(type: "bit", nullable: false),
                    AssumptionVisibility = table.Column<bool>(type: "bit", nullable: false),
                    OptimizationTarget = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptOptimizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptOptimizations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsageTracking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TokensUsed = table.Column<int>(type: "int", nullable: true),
                    ApiCallCount = table.Column<int>(type: "int", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageTracking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageTracking_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultModel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DefaultOptimizationTarget = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Theme = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Density = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MotionEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FavoritePrompts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromptOptimizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoritePrompts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoritePrompts_PromptOptimizations_PromptOptimizationId",
                        column: x => x.PromptOptimizationId,
                        principalTable: "PromptOptimizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FavoritePrompts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_Provider",
                table: "ApiKeys",
                column: "Provider",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoritePrompts_PromptOptimizationId",
                table: "FavoritePrompts",
                column: "PromptOptimizationId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoritePrompts_UserId_PromptOptimizationId",
                table: "FavoritePrompts",
                columns: new[] { "UserId", "PromptOptimizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromptOptimizations_CreatedAt",
                table: "PromptOptimizations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PromptOptimizations_UserId",
                table: "PromptOptimizations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageTracking_UserId",
                table: "UsageTracking",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "FavoritePrompts");

            migrationBuilder.DropTable(
                name: "UsageTracking");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "PromptOptimizations");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
