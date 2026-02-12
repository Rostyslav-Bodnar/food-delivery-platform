using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DF.UserService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImagePublicIdToAccountTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePublicId",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePublicId",
                table: "Accounts");
        }
    }
}
