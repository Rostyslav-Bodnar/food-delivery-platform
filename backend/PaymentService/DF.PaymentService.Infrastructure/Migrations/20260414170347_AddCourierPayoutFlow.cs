using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DF.PaymentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourierPayoutFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "courier_balances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PendingAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AvailableAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    StripeAccountId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PayoutsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courier_balances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "courier_earnings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourierId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    EarnedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvailableAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CourierPayoutId = table.Column<Guid>(type: "uuid", nullable: true),
                    StripeTransferId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courier_earnings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "courier_payouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourierId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StripeTransferId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courier_payouts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_courier_balances_CourierId",
                table: "courier_balances",
                column: "CourierId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_courier_earnings_CourierId_Status_AvailableAtUtc",
                table: "courier_earnings",
                columns: new[] { "CourierId", "Status", "AvailableAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_courier_earnings_OrderId",
                table: "courier_earnings",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_courier_payouts_CourierId_Status",
                table: "courier_payouts",
                columns: new[] { "CourierId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "courier_balances");

            migrationBuilder.DropTable(
                name: "courier_earnings");

            migrationBuilder.DropTable(
                name: "courier_payouts");
        }
    }
}
