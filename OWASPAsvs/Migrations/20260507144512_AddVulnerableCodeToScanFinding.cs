using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OWASPAsvs.Migrations
{
    /// <inheritdoc />
    public partial class AddVulnerableCodeToScanFinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Issue",
                table: "ScanFindings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VulnerableCode",
                table: "ScanFindings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Issue",
                table: "ScanFindings");

            migrationBuilder.DropColumn(
                name: "VulnerableCode",
                table: "ScanFindings");
        }
    }
}
