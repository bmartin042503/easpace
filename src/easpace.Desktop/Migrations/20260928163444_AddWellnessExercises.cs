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
        // the key prefixes old databases store for the seeded techniques (IsLocalized = 1), mapped to the prefixes
        // of the resource keys that now hold their texts; the stored ones are data and must never change
        private static readonly (string StoredPrefix, string ResourcePrefix)[] LocalizedTechniqueKeys =
        [
            ("BreathingTechnique.BoxBreathing", "DefaultExercise.BoxBreathing"),
            ("BreathingTechnique.FourSevenEightBreathing", "DefaultExercise.FourSevenEightBreathing"),
            ("BreathingTechnique.FourSixBreathing", "DefaultExercise.FourSixBreathing"),
            ("BreathingTechnique.TriangleBreathing", "DefaultExercise.TriangleBreathing")
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
                    IsRepeating = table.Column<bool>(type: "INTEGER", nullable: false)
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
                INSERT INTO "WellnessExercises" ("Id", "CreatedAt", "Name", "Description", "IsRepeating")
                SELECT "Id", "CreatedAt", {ResolveLocalizedColumn("Name")}, {ResolveLocalizedColumn("Description")}, 1
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

            // sessions without a technique, like meditations, are named after their session type, as it's dropped below
            migrationBuilder.Sql($"""
                UPDATE "WellnessSessionEntries"
                SET "ExerciseName" = CASE "Type"
                    WHEN 1 THEN {ToSqlLiteral(LocalizationService.GetString("Wellness.SessionType.Meditation"))}
                    ELSE {ToSqlLiteral(LocalizationService.GetString("Wellness.SessionType.Breathing"))}
                END
                WHERE "ExerciseName" IS NULL;
                """);

            // everything of the legacy technique model is copied, so it's removed
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

            // exercises have no type anymore, so the sessions don't either
            migrationBuilder.DropColumn(
                name: "Type",
                table: "WellnessSessionEntries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // the legacy technique model comes back empty, as its data can't be restored
            migrationBuilder.AddColumn<Guid>(
                name: "BreathingTechniqueId",
                table: "WellnessSessionEntries",
                type: "TEXT",
                nullable: true);

            // the session type can't be told apart anymore, so every session comes back as a breathing session
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "WellnessSessionEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

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
            var cases = string.Join(" ", LocalizedTechniqueKeys.Select(keys =>
            {
                var storedKey = $"{keys.StoredPrefix}.{column}";
                var text = LocalizationService.GetString($"{keys.ResourcePrefix}.{column}");
                return $"WHEN {ToSqlLiteral(storedKey)} THEN {ToSqlLiteral(text)}";
            }));

            return $"CASE WHEN \"IsLocalized\" = 1 THEN CASE \"{column}\" {cases} ELSE \"{column}\" END ELSE \"{column}\" END";
        }

        /// <summary>
        /// Converts a value into a SQL string literal, escaping single quotes.
        /// </summary>
        internal static string ToSqlLiteral(string value) => $"'{value.Replace("'", "''")}'";
    }
}
