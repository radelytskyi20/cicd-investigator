using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CiCdInvestigator.Data.Migrations
{
    /// <inheritdoc />
    public partial class SavedReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SavedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RunId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedReports", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedReports");
        }
    }
}
