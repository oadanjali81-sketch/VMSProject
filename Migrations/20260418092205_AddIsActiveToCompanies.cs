using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSProject.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToCompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /* QRCode and Role columns already exist in the database */
            /*
            migrationBuilder.AddColumn<string>(
                name: "QRCode",
                table: "Visitors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
            */
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.DropColumn(
                name: "QRCode",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Companies");
            */
        }
    }
}
