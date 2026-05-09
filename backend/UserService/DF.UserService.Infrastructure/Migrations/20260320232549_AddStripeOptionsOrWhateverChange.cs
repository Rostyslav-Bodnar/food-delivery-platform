using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DF.UserService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeOptionsOrWhateverChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeAccountId",
                table: "BusinessAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "StripeChargesEnabled",
                table: "BusinessAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StripeOnboardedAt",
                table: "BusinessAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StripePayoutsEnabled",
                table: "BusinessAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StripeRequirementsDue",
                table: "BusinessAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeAccountId",
                table: "BusinessAccounts");

            migrationBuilder.DropColumn(
                name: "StripeChargesEnabled",
                table: "BusinessAccounts");

            migrationBuilder.DropColumn(
                name: "StripeOnboardedAt",
                table: "BusinessAccounts");

            migrationBuilder.DropColumn(
                name: "StripePayoutsEnabled",
                table: "BusinessAccounts");

            migrationBuilder.DropColumn(
                name: "StripeRequirementsDue",
                table: "BusinessAccounts");
        }
    }
}
