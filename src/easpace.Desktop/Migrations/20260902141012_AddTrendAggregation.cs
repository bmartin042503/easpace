using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace easpace.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class AddTrendAggregation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Aggregation",
                table: "Activities",
                type: "INTEGER",
                nullable: true,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Aggregation",
                table: "Activities");
        }
    }
}
