using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASVSGuard.Migrations
{
    /// <inheritdoc />
    public partial class AddScanFindingLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Line",
                table: "ScanFindings",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Line",
                table: "ScanFindings");
        }
    }
}
