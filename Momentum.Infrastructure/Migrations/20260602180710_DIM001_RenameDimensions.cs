using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Momentum.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DIM001_RenameDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Dimensions",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Body");

            migrationBuilder.UpdateData(
                table: "Dimensions",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Mind");

            migrationBuilder.UpdateData(
                table: "Dimensions",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Spirit");

            migrationBuilder.UpdateData(
                table: "Dimensions",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Connections");

            migrationBuilder.UpdateData(
                table: "Dimensions",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "Responsibilities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Dimensions",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Physical");

            migrationBuilder.UpdateData(
                table: "Dimensions",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Mental");

            migrationBuilder.UpdateData(
                table: "Dimensions",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Spiritual");

            migrationBuilder.UpdateData(
                table: "Dimensions",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Social");

            migrationBuilder.UpdateData(
                table: "Dimensions",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "Housekeeping");
        }
    }
}
