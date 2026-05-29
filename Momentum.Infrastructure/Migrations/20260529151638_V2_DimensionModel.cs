using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Momentum.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V2_DimensionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Rename tables (DDL metadata ops — no data movement, no locking) ──────
            migrationBuilder.RenameTable(
                name:    "Categories",
                newName: "Dimensions");

            migrationBuilder.RenameTable(
                name:    "ActivityCategories",
                newName: "ActivityDimensions");

            // ── 2. Rename column in ActivityDimensions ───────────────────────────────────
            migrationBuilder.RenameColumn(
                name:    "CategoryId",
                table:   "ActivityDimensions",
                newName: "DimensionId");

            // ── 3. Drop old FK constraints and indexes ────────────────────────────────────
            migrationBuilder.DropForeignKey(
                name:  "FK_ActivityCategories_Categories_CategoryId",
                table: "ActivityDimensions");

            migrationBuilder.DropForeignKey(
                name:  "FK_ActivityCategories_Activities_ActivityId",
                table: "ActivityDimensions");

            migrationBuilder.DropPrimaryKey(
                name:  "PK_ActivityCategories",
                table: "ActivityDimensions");

            migrationBuilder.DropIndex(
                name:  "IX_ActivityCategories_CategoryId",
                table: "ActivityDimensions");

            // ── 4. Recreate constraints with new names ────────────────────────────────────
            migrationBuilder.AddPrimaryKey(
                name:    "PK_ActivityDimensions",
                table:   "ActivityDimensions",
                columns: new[] { "ActivityId", "DimensionId" });

            migrationBuilder.CreateIndex(
                name:   "IX_ActivityDimensions_DimensionId",
                table:  "ActivityDimensions",
                column: "DimensionId");

            migrationBuilder.AddForeignKey(
                name:            "FK_ActivityDimensions_Activities_ActivityId",
                table:           "ActivityDimensions",
                column:          "ActivityId",
                principalTable:  "Activities",
                principalColumn: "Id",
                onDelete:        ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name:            "FK_ActivityDimensions_Dimensions_DimensionId",
                table:           "ActivityDimensions",
                column:          "DimensionId",
                principalTable:  "Dimensions",
                principalColumn: "Id",
                onDelete:        ReferentialAction.Restrict);

            // ── 5. Create ActivityLogEntryDimensions (new table) ─────────────────────────
            migrationBuilder.CreateTable(
                name: "ActivityLogEntryDimensions",
                columns: table => new
                {
                    ActivityLogId = table.Column<int>(type: "int", nullable: false),
                    DimensionId   = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_ActivityLogEntryDimensions",
                        x => new { x.ActivityLogId, x.DimensionId });

                    table.ForeignKey(
                        name:            "FK_ActivityLogEntryDimensions_ActivityLogs_ActivityLogId",
                        column:          x => x.ActivityLogId,
                        principalTable:  "ActivityLogs",
                        principalColumn: "Id",
                        onDelete:        ReferentialAction.Cascade);

                    table.ForeignKey(
                        name:            "FK_ActivityLogEntryDimensions_Dimensions_DimensionId",
                        column:          x => x.DimensionId,
                        principalTable:  "Dimensions",
                        principalColumn: "Id",
                        onDelete:        ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name:   "IX_ActivityLogEntryDimensions_DimensionId",
                table:  "ActivityLogEntryDimensions",
                column: "DimensionId");

            // ── 6. Backfill: populate ActivityLogEntryDimensions from existing data ───────
            // One row per (ActivityLog, Dimension) pair via the activity's current assignments.
            // Uses INNER JOIN — logs without any dimension assignment produce no rows (same as
            // current derived-join behaviour).
            migrationBuilder.Sql(@"
                INSERT INTO ActivityLogEntryDimensions (ActivityLogId, DimensionId)
                SELECT al.Id  AS ActivityLogId,
                       ad.DimensionId
                FROM   ActivityLogs      al
                JOIN   ActivityDimensions ad ON ad.ActivityId = al.ActivityId;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ActivityLogEntryDimensions");

            migrationBuilder.DropForeignKey(
                name:  "FK_ActivityDimensions_Activities_ActivityId",
                table: "ActivityDimensions");
            migrationBuilder.DropForeignKey(
                name:  "FK_ActivityDimensions_Dimensions_DimensionId",
                table: "ActivityDimensions");
            migrationBuilder.DropPrimaryKey(
                name:  "PK_ActivityDimensions",
                table: "ActivityDimensions");
            migrationBuilder.DropIndex(
                name:  "IX_ActivityDimensions_DimensionId",
                table: "ActivityDimensions");

            migrationBuilder.RenameColumn(
                name:    "DimensionId",
                table:   "ActivityDimensions",
                newName: "CategoryId");

            migrationBuilder.AddPrimaryKey(
                name:    "PK_ActivityCategories",
                table:   "ActivityDimensions",
                columns: new[] { "ActivityId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name:   "IX_ActivityCategories_CategoryId",
                table:  "ActivityDimensions",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name:            "FK_ActivityCategories_Activities_ActivityId",
                table:           "ActivityDimensions",
                column:          "ActivityId",
                principalTable:  "Activities",
                principalColumn: "Id",
                onDelete:        ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name:            "FK_ActivityCategories_Categories_CategoryId",
                table:           "ActivityDimensions",
                column:          "CategoryId",
                principalTable:  "Dimensions",       // still named Dimensions after rename
                principalColumn: "Id",
                onDelete:        ReferentialAction.Restrict);

            migrationBuilder.RenameTable(
                name:    "ActivityDimensions",
                newName: "ActivityCategories");

            migrationBuilder.RenameTable(
                name:    "Dimensions",
                newName: "Categories");
        }
    }
}
