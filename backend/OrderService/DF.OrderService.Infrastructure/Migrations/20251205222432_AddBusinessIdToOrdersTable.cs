using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DF.OrderService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessIdToOrdersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId",
                table: "Orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "Orders");
        }
    }
}
