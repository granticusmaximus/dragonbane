using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndCompanion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Species = table.Column<string>(type: "TEXT", nullable: false),
                    Class = table.Column<string>(type: "TEXT", nullable: false),
                    Subclass = table.Column<string>(type: "TEXT", nullable: true),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Background = table.Column<string>(type: "TEXT", nullable: true),
                    ProficiencyBonus = table.Column<int>(type: "INTEGER", nullable: false),
                    Abilities = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Attribution = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CampaignId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogs_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampaignCharacter",
                columns: table => new
                {
                    CampaignId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignCharacter", x => new { x.CampaignId, x.CharacterId });
                    table.ForeignKey(
                        name: "FK_CampaignCharacter_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignCharacter_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionDefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", nullable: false),
                    RulesText = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionDefs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionDefs_ContentSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ContentSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    WeightLb = table.Column<double>(type: "REAL", nullable: true),
                    Cost = table.Column<string>(type: "TEXT", nullable: true),
                    PropertiesJson = table.Column<string>(type: "TEXT", nullable: true),
                    RulesText = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_ContentSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ContentSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Spells",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    School = table.Column<string>(type: "TEXT", nullable: false),
                    CastingTime = table.Column<string>(type: "TEXT", nullable: false),
                    Range = table.Column<string>(type: "TEXT", nullable: false),
                    Components = table.Column<string>(type: "TEXT", nullable: false),
                    Duration = table.Column<string>(type: "TEXT", nullable: false),
                    Concentration = table.Column<bool>(type: "INTEGER", nullable: false),
                    RulesText = table.Column<string>(type: "TEXT", nullable: true),
                    ScalingJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Spells_ContentSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ContentSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionLogId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RoundNo = table.Column<int>(type: "INTEGER", nullable: true),
                    InitiativeSlot = table.Column<int>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    DiceResultJson = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionEntries_SessionLogs_SessionLogId",
                        column: x => x.SessionLogId,
                        principalTable: "SessionLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recordings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionLogId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AudioPath = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recordings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recordings_SessionLogs_SessionLogId",
                        column: x => x.SessionLogId,
                        principalTable: "SessionLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterAction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActionDefId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterAction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterAction_ActionDefs_ActionDefId",
                        column: x => x.ActionDefId,
                        principalTable: "ActionDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterAction_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Equipped = table.Column<bool>(type: "INTEGER", nullable: false),
                    Attuned = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterItem_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterItem_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSpell",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SpellId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsPrepared = table.Column<bool>(type: "INTEGER", nullable: false),
                    AlwaysPrepared = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSpell", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterSpell_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterSpell_Spells_SpellId",
                        column: x => x.SpellId,
                        principalTable: "Spells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StructuredNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecordingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TStart = table.Column<double>(type: "REAL", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Confirmed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StructuredNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StructuredNotes_Recordings_RecordingId",
                        column: x => x.RecordingId,
                        principalTable: "Recordings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TranscriptSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecordingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TStart = table.Column<double>(type: "REAL", nullable: false),
                    TEnd = table.Column<double>(type: "REAL", nullable: false),
                    SpeakerLabel = table.Column<string>(type: "TEXT", nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscriptSegments_Recordings_RecordingId",
                        column: x => x.RecordingId,
                        principalTable: "Recordings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionDefs_SourceId",
                table: "ActionDefs",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionEntries_SessionLogId_CreatedUtc",
                table: "ActionEntries",
                columns: new[] { "SessionLogId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignCharacter_CharacterId",
                table: "CampaignCharacter",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAction_ActionDefId",
                table: "CharacterAction",
                column: "ActionDefId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAction_CharacterId",
                table: "CharacterAction",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterItem_CharacterId",
                table: "CharacterItem",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterItem_ItemId",
                table: "CharacterItem",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSpell_CharacterId",
                table: "CharacterSpell",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSpell_SpellId",
                table: "CharacterSpell",
                column: "SpellId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Category_Name",
                table: "Items",
                columns: new[] { "Category", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_SourceId",
                table: "Items",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_SessionLogId",
                table: "Recordings",
                column: "SessionLogId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_CampaignId",
                table: "SessionLogs",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_Spells_Level_Name",
                table: "Spells",
                columns: new[] { "Level", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Spells_SourceId",
                table: "Spells",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_StructuredNotes_RecordingId",
                table: "StructuredNotes",
                column: "RecordingId");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSegments_RecordingId",
                table: "TranscriptSegments",
                column: "RecordingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionEntries");

            migrationBuilder.DropTable(
                name: "CampaignCharacter");

            migrationBuilder.DropTable(
                name: "CharacterAction");

            migrationBuilder.DropTable(
                name: "CharacterItem");

            migrationBuilder.DropTable(
                name: "CharacterSpell");

            migrationBuilder.DropTable(
                name: "StructuredNotes");

            migrationBuilder.DropTable(
                name: "TranscriptSegments");

            migrationBuilder.DropTable(
                name: "ActionDefs");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Spells");

            migrationBuilder.DropTable(
                name: "Recordings");

            migrationBuilder.DropTable(
                name: "ContentSources");

            migrationBuilder.DropTable(
                name: "SessionLogs");

            migrationBuilder.DropTable(
                name: "Campaigns");
        }
    }
}
