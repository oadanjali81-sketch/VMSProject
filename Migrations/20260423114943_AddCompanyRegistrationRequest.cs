using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSProject.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyRegistrationRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyRegistrationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactPersonName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompanyAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyRegistrationRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_CreatedBy",
                table: "Visits",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Companies_CreatedBy",
                table: "Visits",
                column: "CreatedBy",
                principalTable: "Companies",
                principalColumn: "CompanyId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Companies_CreatedBy",
                table: "Visits");

            migrationBuilder.DropTable(
                name: "CompanyRegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_Visits_CreatedBy",
                table: "Visits");
        }
    }
}
