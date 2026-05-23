using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DF.UserService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProvisioningRetryAndRefreshConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Accounts_AccountId",
                table: "Users");

            // NOTE: `xmin` is a Postgres system column that already exists on
            // every table. The model declares it as a concurrency token via a
            // shadow property in AppDbContext; we do NOT create a real column
            // for it. The scaffolder generated an AddColumn here that has
            // been removed manually.

            migrationBuilder.AddColumn<int>(
                name: "StripeProvisioningAttempts",
                table: "BusinessAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StripeProvisioningLastAttemptUtc",
                table: "BusinessAccounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeProvisioningLastError",
                table: "BusinessAccounts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAccounts_StripeAccountId",
                table: "BusinessAccounts",
                column: "StripeAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAccounts_StripeProvisioningLastAttemptUtc",
                table: "BusinessAccounts",
                column: "StripeProvisioningLastAttemptUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Accounts_AccountId",
                table: "Users",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Accounts_AccountId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_BusinessAccounts_StripeAccountId",
                table: "BusinessAccounts");

            migrationBuilder.DropIndex(
                name: "IX_BusinessAccounts_StripeProvisioningLastAttemptUtc",
                table: "BusinessAccounts");

            // See Up(): xmin is a Postgres system column; no DropColumn here.

            migrationBuilder.DropColumn(
                name: "StripeProvisioningAttempts",
                table: "BusinessAccounts");

            migrationBuilder.DropColumn(
                name: "StripeProvisioningLastAttemptUtc",
                table: "BusinessAccounts");

            migrationBuilder.DropColumn(
                name: "StripeProvisioningLastError",
                table: "BusinessAccounts");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Accounts_AccountId",
                table: "Users",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
