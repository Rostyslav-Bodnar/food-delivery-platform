using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DF.TrackingService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LocationOrderIdAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "Locations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_GeoPoint",
                table: "Locations",
                column: "GeoPoint")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_OrderId",
                table: "Locations",
                column: "OrderId",
                unique: true,
                filter: "\"OrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessLocations_BusinessId",
                table: "BusinessLocations",
                column: "BusinessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Locations_GeoPoint",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Locations_OrderId",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_BusinessLocations_BusinessId",
                table: "BusinessLocations");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Locations");
        }
    }
}
