using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DF.MenuService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIngredient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ingredients_Dishes_DishId1",
                table: "Ingredients");

            migrationBuilder.DropIndex(
                name: "IX_Ingredients_DishId1",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "DishId1",
                table: "Ingredients");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DishId1",
                table: "Ingredients",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_DishId1",
                table: "Ingredients",
                column: "DishId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Ingredients_Dishes_DishId1",
                table: "Ingredients",
                column: "DishId1",
                principalTable: "Dishes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
