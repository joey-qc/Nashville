using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Momentum.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorCategoryToEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Category",
                table: "ActivityCategories",
                newName: "CategoryId");

            // Enum values were 0-4; new Category table IDs are 1-5
            migrationBuilder.Sql("UPDATE ActivityCategories SET CategoryId = CategoryId + 1");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ColorHex = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "ColorHex", "Name" },
                values: new object[,]
                {
                    { 1, "#4CAF50", "Physical" },
                    { 2, "#2196F3", "Mental" },
                    { 3, "#9C27B0", "Spiritual" },
                    { 4, "#FF9800", "Social" },
                    { 5, "#FFC107", "Housekeeping" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityCategories_CategoryId",
                table: "ActivityCategories",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityCategories_Categories_CategoryId",
                table: "ActivityCategories",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityCategories_Categories_CategoryId",
                table: "ActivityCategories");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_ActivityCategories_CategoryId",
                table: "ActivityCategories");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "ActivityCategories",
                newName: "Category");

            // Restore enum values 0-4 from IDs 1-5
            migrationBuilder.Sql("UPDATE ActivityCategories SET [Category] = [Category] - 1");
        }
    }
}
