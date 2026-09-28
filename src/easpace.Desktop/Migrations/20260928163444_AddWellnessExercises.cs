using System;
using System.Linq;
using easpace.Desktop.Services.Core;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace easpace.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class AddWellnessExercises : Migration
    {
        // localization key prefixes of the seeded techniques stored with IsLocalized = 1
        private static readonly string[] LocalizedTechniqueKeys =
        [
            "BreathingTechnique.BoxBreathing",
            "BreathingTechnique.FourSevenEightBreathing",
            "BreathingTechnique.FourSixBreathing",
            "BreathingTechnique.TriangleBreathing"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExerciseId",
                table: "WellnessSessionEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExerciseName",
                table: "WellnessSessionEntries",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WellnessExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsRepeating = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExerciseType = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WellnessExercises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseInstructions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Phase = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseInstructions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseInstructions_WellnessExercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "WellnessExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WellnessSessionEntries_ExerciseId",
                table: "WellnessSessionEntries",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseInstructions_ExerciseId",
                table: "ExerciseInstructions",
                column: "ExerciseId");

            migrationBuilder.AddForeignKey(
                name: "FK_WellnessSessionEntries_WellnessExercises_ExerciseId",
                table: "WellnessSessionEntries",
                column: "ExerciseId",
                principalTable: "WellnessExercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // copy the legacy breathing techniques, resolving localization keys in the current app language
            migrationBuilder.Sql($"""
                INSERT INTO "WellnessExercises" ("Id", "ExerciseType", "CreatedAt", "Name", "Description", "IsRepeating")
                SELECT "Id", 'Breathing', "CreatedAt", {ResolveLocalizedColumn("Name")}, {ResolveLocalizedColumn("Description")}, 1
                FROM "BreathingTechniques";
                """);

            // phases become instructions without text, so the phase's default text is shown at runtime
            migrationBuilder.Sql("""
                INSERT INTO "ExerciseInstructions" ("Id", "ExerciseId", "Order", "Text", "DurationSeconds", "Phase")
                SELECT "Id", "BreathingTechniqueId", "Order", '', "DurationSeconds", "Type"
                FROM "BreathingPhases"
                WHERE "BreathingTechniqueId" IS NOT NULL;
                """);

            // technique ids are preserved, so existing sessions can point at the copied exercises
            migrationBuilder.Sql("""
                UPDATE "WellnessSessionEntries"
                SET "ExerciseId" = "BreathingTechniqueId",
                    "ExerciseName" = (SELECT e."Name" FROM "WellnessExercises" e WHERE e."Id" = "WellnessSessionEntries"."BreathingTechniqueId")
                WHERE "BreathingTechniqueId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WellnessSessionEntries_WellnessExercises_ExerciseId",
                table: "WellnessSessionEntries");

            migrationBuilder.DropTable(
                name: "ExerciseInstructions");

            migrationBuilder.DropTable(
                name: "WellnessExercises");

            migrationBuilder.DropIndex(
                name: "IX_WellnessSessionEntries_ExerciseId",
                table: "WellnessSessionEntries");

            migrationBuilder.DropColumn(
                name: "ExerciseId",
                table: "WellnessSessionEntries");

            migrationBuilder.DropColumn(
                name: "ExerciseName",
                table: "WellnessSessionEntries");
        }

        /// <summary>
        /// Builds a SQL expression that replaces known localization keys of localized techniques with their resolved text.
        /// </summary>
        /// <param name="column">The BreathingTechniques column holding either a localization key or plain text.</param>
        private static string ResolveLocalizedColumn(string column)
        {
            var cases = string.Join(" ", LocalizedTechniqueKeys.Select(prefix =>
            {
                var key = $"{prefix}.{column}";
                return $"WHEN {ToSqlLiteral(key)} THEN {ToSqlLiteral(LocalizationService.GetString(key))}";
            }));

            return $"CASE WHEN \"IsLocalized\" = 1 THEN CASE \"{column}\" {cases} ELSE \"{column}\" END ELSE \"{column}\" END";
        }

        /// <summary>
        /// Converts a value into a SQL string literal, escaping single quotes.
        /// </summary>
        internal static string ToSqlLiteral(string value) => $"'{value.Replace("'", "''")}'";
    }
}
