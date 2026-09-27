using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PromptForge.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class QueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UsageTracking_UserId",
                table: "UsageTracking");

            migrationBuilder.DropIndex(
                name: "IX_PromptOptimizations_CreatedAt",
                table: "PromptOptimizations");

            migrationBuilder.DropIndex(
                name: "IX_PromptOptimizations_UserId",
                table: "PromptOptimizations");

            migrationBuilder.CreateIndex(
                name: "IX_UsageTracking_UserId_Date",
                table: "UsageTracking",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_PromptOptimizations_UserId_CreatedAt",
                table: "PromptOptimizations",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UsageTracking_UserId_Date",
                table: "UsageTracking");

            migrationBuilder.DropIndex(
                name: "IX_PromptOptimizations_UserId_CreatedAt",
                table: "PromptOptimizations");

            migrationBuilder.CreateIndex(
                name: "IX_UsageTracking_UserId",
                table: "UsageTracking",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptOptimizations_CreatedAt",
                table: "PromptOptimizations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PromptOptimizations_UserId",
                table: "PromptOptimizations",
                column: "UserId");
        }
    }
}
