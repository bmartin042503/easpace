using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace easpace.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBreathingTechniques : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // sessions saved after AddWellnessExercises but before sessions ran on exercises only reference their technique;
            // technique ids were kept as exercise ids, so link them before the column is dropped. Only existing exercises
            // are linked, as a dangling ExerciseId would violate its foreign key and fail the whole migration.
            migrationBuilder.Sql("""
                UPDATE "WellnessSessionEntries"
                SET "ExerciseId" = "BreathingTechniqueId",
                    "ExerciseName" = COALESCE("ExerciseName",
                        (SELECT e."Name" FROM "WellnessExercises" e WHERE e."Id" = "WellnessSessionEntries"."BreathingTechniqueId"))
                WHERE "ExerciseId" IS NULL
                  AND "BreathingTechniqueId" IN (SELECT "Id" FROM "WellnessExercises");
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_WellnessSessionEntries_BreathingTechniques_BreathingTechniqueId",
                table: "WellnessSessionEntries");

            migrationBuilder.DropTable(
                name: "BreathingPhases");

            migrationBuilder.DropTable(
                name: "BreathingTechniques");

            migrationBuilder.DropIndex(
                name: "IX_WellnessSessionEntries_BreathingTechniqueId",
                table: "WellnessSessionEntries");

            migrationBuilder.DropColumn(
                name: "BreathingTechniqueId",
                table: "WellnessSessionEntries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BreathingTechniqueId",
                table: "WellnessSessionEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BreathingTechniques",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Cycles = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsLocalized = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreathingTechniques", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BreathingPhases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BreathingTechniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreathingPhases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BreathingPhases_BreathingTechniques_BreathingTechniqueId",
                        column: x => x.BreathingTechniqueId,
                        principalTable: "BreathingTechniques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WellnessSessionEntries_BreathingTechniqueId",
                table: "WellnessSessionEntries",
                column: "BreathingTechniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_BreathingPhases_BreathingTechniqueId",
                table: "BreathingPhases",
                column: "BreathingTechniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_WellnessSessionEntries_BreathingTechniques_BreathingTechniqueId",
                table: "WellnessSessionEntries",
                column: "BreathingTechniqueId",
                principalTable: "BreathingTechniques",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
