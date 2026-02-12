using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DF.MenuService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeightFieldToIngredientsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Weight",
                table: "Ingredients",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Ingredients");
        }
    }
}
