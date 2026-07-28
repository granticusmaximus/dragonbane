using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndCompanion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonsterTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonsterTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    HpMax = table.Column<int>(type: "INTEGER", nullable: false),
                    ArmorClass = table.Column<int>(type: "INTEGER", nullable: false),
                    InitiativeBonus = table.Column<int>(type: "INTEGER", nullable: false),
                    StatBlockText = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonsterTemplates", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonsterTemplates");
        }
    }
}
