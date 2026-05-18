using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCK26.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    PerformedBy = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChangedFields = table.Column<string>(type: "TEXT", nullable: true),
                    EntryDate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    WorksheetId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_WorksheetId_EntryDate_PerformedAt",
                table: "AuditLogs",
                columns: new[] { "WorksheetId", "EntryDate", "PerformedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");
        }
    }
}
