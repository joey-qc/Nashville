using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Momentum.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CHK001_AddCheckIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheckIns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BodyScore = table.Column<int>(type: "int", nullable: false),
                    EnergyScore = table.Column<int>(type: "int", nullable: false),
                    MoodScore = table.Column<int>(type: "int", nullable: false),
                    ActivityLogId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckIns_ActivityLogs_ActivityLogId",
                        column: x => x.ActivityLogId,
                        principalTable: "ActivityLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_ActivityLogId",
                table: "CheckIns",
                column: "ActivityLogId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_CheckedInAt",
                table: "CheckIns",
                column: "CheckedInAt");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_UserId",
                table: "CheckIns",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckIns");
        }
    }
}
