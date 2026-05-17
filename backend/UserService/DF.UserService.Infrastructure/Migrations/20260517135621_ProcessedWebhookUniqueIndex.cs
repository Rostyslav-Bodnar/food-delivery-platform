using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DF.UserService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProcessedWebhookUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WebhookId",
                table: "ProcessedWebhooks",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedWebhooks_WebhookId",
                table: "ProcessedWebhooks",
                column: "WebhookId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessedWebhooks_WebhookId",
                table: "ProcessedWebhooks");

            migrationBuilder.AlterColumn<string>(
                name: "WebhookId",
                table: "ProcessedWebhooks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }
    }
}
