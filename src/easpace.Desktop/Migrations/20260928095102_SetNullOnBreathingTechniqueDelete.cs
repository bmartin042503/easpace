using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace easpace.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class SetNullOnBreathingTechniqueDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WellnessSessionEntries_BreathingTechniques_BreathingTechniqueId",
                table: "WellnessSessionEntries");

            migrationBuilder.AddForeignKey(
                name: "FK_WellnessSessionEntries_BreathingTechniques_BreathingTechniqueId",
                table: "WellnessSessionEntries",
                column: "BreathingTechniqueId",
                principalTable: "BreathingTechniques",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WellnessSessionEntries_BreathingTechniques_BreathingTechniqueId",
                table: "WellnessSessionEntries");

            migrationBuilder.AddForeignKey(
                name: "FK_WellnessSessionEntries_BreathingTechniques_BreathingTechniqueId",
                table: "WellnessSessionEntries",
                column: "BreathingTechniqueId",
                principalTable: "BreathingTechniques",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
