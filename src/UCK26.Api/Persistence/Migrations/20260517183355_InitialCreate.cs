using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCK26.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Users" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
                    "UserName" TEXT NOT NULL,
                    "PasswordHash" TEXT NOT NULL,
                    "Role" TEXT NOT NULL,
                    "CreatedAt" TEXT NOT NULL
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Worksheets" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Worksheets" PRIMARY KEY AUTOINCREMENT,
                    "UserId" INTEGER NOT NULL,
                    "UserName" TEXT NOT NULL,
                    "Year" INTEGER NOT NULL,
                    "Month" INTEGER NOT NULL
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "WorkEntries" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_WorkEntries" PRIMARY KEY AUTOINCREMENT,
                    "WorksheetId" INTEGER NOT NULL,
                    "Date" TEXT NOT NULL,
                    "Type" TEXT NOT NULL,
                    "Hours" TEXT NOT NULL,
                    CONSTRAINT "FK_WorkEntries_Worksheets_WorksheetId"
                        FOREIGN KEY ("WorksheetId") REFERENCES "Worksheets" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO "Users" ("Id", "CreatedAt", "PasswordHash", "Role", "UserName")
                VALUES (1, '2026-01-01 00:00:00', 'seed:Demo!2026', 'Admin', 'admin')
                ON CONFLICT("Id") DO UPDATE SET
                    "PasswordHash" = excluded."PasswordHash",
                    "Role" = excluded."Role",
                    "UserName" = excluded."UserName";
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_UserName"
                ON "Users" ("UserName");
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_WorkEntries_WorksheetId_Date_Type"
                ON "WorkEntries" ("WorksheetId", "Date", "Type");
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Worksheets_UserId_Year_Month"
                ON "Worksheets" ("UserId", "Year", "Month");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "WorkEntries");

            migrationBuilder.DropTable(
                name: "Worksheets");
        }
    }
}
