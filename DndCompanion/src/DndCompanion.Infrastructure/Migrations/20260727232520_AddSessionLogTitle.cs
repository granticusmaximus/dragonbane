using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndCompanion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionLogTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "SessionLogs",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "SessionLogs");
        }
    }
}
