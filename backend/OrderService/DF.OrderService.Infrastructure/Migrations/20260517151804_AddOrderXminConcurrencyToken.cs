using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DF.OrderService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderXminConcurrencyToken : Migration
    {
        // xmin is a Postgres system column on every table; nothing to add at the schema level.
        // The migration is kept as a no-op so EF's model snapshot stays consistent.
        protected override void Up(MigrationBuilder migrationBuilder) { }
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
