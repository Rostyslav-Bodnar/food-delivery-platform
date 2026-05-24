using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DF.PaymentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDestinationStripeAccountIdToPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DestinationStripeAccountId",
                table: "payments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinationStripeAccountId",
                table: "payments");
        }
    }
}
