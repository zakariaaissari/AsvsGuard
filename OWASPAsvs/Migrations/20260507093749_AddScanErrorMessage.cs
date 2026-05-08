using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OWASPAsvs.Migrations
{
    /// <inheritdoc />
    public partial class AddScanErrorMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "RepoScans",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "RepoScans");
        }
    }
}
