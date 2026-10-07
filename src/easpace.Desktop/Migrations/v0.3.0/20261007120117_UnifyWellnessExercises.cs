using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace easpace.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class UnifyWellnessExercises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WellnessExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "TEXT",
                        nullable: false),

                    CreatedAt = table.Column<DateTimeOffset>(
                        type: "TEXT",
                        nullable: false),

                    Name = table.Column<string>(
                        type: "TEXT",
                        maxLength: 64,
                        nullable: false),

                    Description = table.Column<string>(
                        type: "TEXT",
                        maxLength: 512,
                        nullable: false),

                    DefaultCycleCount = table.Column<int>(
                        type: "INTEGER",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WellnessExercises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WellnessExerciseInstructions",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "TEXT",
                        nullable: false),

                    ExerciseId = table.Column<Guid>(
                        type: "TEXT",
                        nullable: false),

                    Order = table.Column<int>(
                        type: "INTEGER",
                        nullable: false),

                    Text = table.Column<string>(
                        type: "TEXT",
                        maxLength: 256,
                        nullable: false),

                    DurationSeconds = table.Column<int>(
                        type: "INTEGER",
                        nullable: false),

                    BreathingPhase = table.Column<int>(
                        type: "INTEGER",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_WellnessExerciseInstructions",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_WellnessExerciseInstructions_WellnessExercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "WellnessExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WellnessExerciseInstructions_ExerciseId_Order",
                table: "WellnessExerciseInstructions",
                columns: new[] { "ExerciseId", "Order" });

            migrationBuilder.Sql("""
                -- Create the replacement session table.
                CREATE TABLE "__WellnessSessionEntries_v030" (
                    "Id" TEXT NOT NULL,
                    "StartDate" TEXT NOT NULL,
                    "Duration" TEXT NOT NULL,
                    "CycleCount" INTEGER NULL,
                    "ExerciseId" TEXT NULL,
                    "ExerciseName" TEXT NOT NULL,

                    CONSTRAINT "PK_WellnessSessionEntries"
                        PRIMARY KEY ("Id"),

                    CONSTRAINT "FK_WellnessSessionEntries_WellnessExercises_ExerciseId"
                        FOREIGN KEY ("ExerciseId")
                        REFERENCES "WellnessExercises" ("Id")
                        ON DELETE SET NULL
                );

                -- Preserve every session.
                -- Techniques are used only to capture historical names.
                -- No legacy exercises or phases are copied.
                INSERT INTO "__WellnessSessionEntries_v030"
                    ("Id", "StartDate", "Duration", "CycleCount",
                     "ExerciseId", "ExerciseName")
                SELECT
                    s."Id",
                    s."StartDate",
                    s."ActualDuration",
                    NULL,
                    NULL,
                    COALESCE(
                        CASE WHEN t."IsLocalized" = 1 THEN
                            CASE t."Name"
                                WHEN 'BreathingTechnique.BoxBreathing.Name'
                                    THEN 'Box Breathing'
                                WHEN 'BreathingTechnique.FourSevenEightBreathing.Name'
                                    THEN '4-7-8 Breathing'
                                WHEN 'BreathingTechnique.FourSixBreathing.Name'
                                    THEN '4-6 Breathing'
                                WHEN 'BreathingTechnique.TriangleBreathing.Name'
                                    THEN 'Triangle Breathing'
                                ELSE t."Name"
                            END
                        ELSE t."Name" END,
                        CASE s."Type"
                            WHEN 1 THEN 'Meditation'
                            WHEN 0 THEN 'Breathing'
                            ELSE 'Wellness session'
                        END
                    )
                FROM "WellnessSessionEntries" AS s
                LEFT JOIN "BreathingTechniques" AS t
                    ON t."Id" = s."BreathingTechniqueId";

                -- Replace the legacy session table after copying its data.
                DROP TABLE "WellnessSessionEntries";

                ALTER TABLE "__WellnessSessionEntries_v030"
                    RENAME TO "WellnessSessionEntries";

                CREATE INDEX "IX_WellnessSessionEntries_ExerciseId"
                    ON "WellnessSessionEntries" ("ExerciseId");

                -- Historical names have been captured; remove legacy exercises.
                DROP TABLE "BreathingPhases";
                DROP TABLE "BreathingTechniques";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("This migration cannot restore the legacy wellness data model.");
        }
    }
}