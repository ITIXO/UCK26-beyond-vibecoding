using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCK26.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkEntryTimeRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_WorksheetId_Date_Type",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "Hours",
                table: "WorkEntries");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "WorkEntries",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "End",
                table: "WorkEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "Start",
                table: "WorkEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_WorksheetId_Date_Type",
                table: "WorkEntries",
                columns: new[] { "WorksheetId", "Date", "Type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_WorksheetId_Date_Type",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "End",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "Start",
                table: "WorkEntries");

            migrationBuilder.AddColumn<decimal>(
                name: "Hours",
                table: "WorkEntries",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_WorksheetId_Date_Type",
                table: "WorkEntries",
                columns: new[] { "WorksheetId", "Date", "Type" },
                unique: true);
        }
    }
}
