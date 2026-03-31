using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DF.UserService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayoutRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StripeAccountId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StripePayoutId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstimatedArrivalUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BalanceTransactionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutRecords_BusinessAccounts_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "BusinessAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedWebhooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebhookId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedWebhooks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRecords_BusinessId",
                table: "PayoutRecords",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRecords_CreatedAtUtc",
                table: "PayoutRecords",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRecords_Status",
                table: "PayoutRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRecords_StripeAccountId",
                table: "PayoutRecords",
                column: "StripeAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRecords_StripePayoutId",
                table: "PayoutRecords",
                column: "StripePayoutId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayoutRecords");

            migrationBuilder.DropTable(
                name: "ProcessedWebhooks");
        }
    }
}
