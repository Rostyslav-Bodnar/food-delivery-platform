using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DF.MenuService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDishXminConcurrencyToken : Migration
    {
        // Postgres' xmin is a system column present on every table; nothing to add at the
        // schema level. We keep the migration as a no-op so EF's model snapshot stays in sync.
        protected override void Up(MigrationBuilder migrationBuilder) { }
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
