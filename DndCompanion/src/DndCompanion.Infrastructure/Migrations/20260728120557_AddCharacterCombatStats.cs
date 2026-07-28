using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndCompanion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterCombatStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Alignment",
                table: "Characters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArmorClass",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "CurrentHp",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExpertSkills",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<int>(
                name: "InitiativeBonus",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxHp",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProficientSaves",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "ProficientSkills",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "Medium");

            migrationBuilder.AddColumn<int>(
                name: "Speed",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<string>(
                name: "SpellSlots",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "{\"Level1Current\":0,\"Level1Max\":0,\"Level2Current\":0,\"Level2Max\":0,\"Level3Current\":0,\"Level3Max\":0,\"Level4Current\":0,\"Level4Max\":0,\"Level5Current\":0,\"Level5Max\":0,\"Level6Current\":0,\"Level6Max\":0,\"Level7Current\":0,\"Level7Max\":0,\"Level8Current\":0,\"Level8Max\":0,\"Level9Current\":0,\"Level9Max\":0}");

            migrationBuilder.AddColumn<int>(
                name: "TempHp",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Alignment",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ArmorClass",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CurrentHp",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ExpertSkills",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "InitiativeBonus",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "MaxHp",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ProficientSaves",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ProficientSkills",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Speed",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "SpellSlots",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "TempHp",
                table: "Characters");
        }
    }
}
