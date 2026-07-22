using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FridgeMealPlanner.Migrations
{
    /// <inheritdoc />
    public partial class PivotToBudgeting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PricePerUnit",
                table: "Ingredients",
                type: "numeric(12,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriceUnit",
                table: "Ingredients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "CookedMeals",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ItemCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecipeBookmarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RecipeId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeBookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeBookmarks_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PricePerUnit", "PriceUnit" },
                values: new object[] { 12m, 6 });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PricePerUnit", "PriceUnit" },
                values: new object[] { 2.5m, 6 });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PricePerUnit", "PriceUnit" },
                values: new object[] { 0.5m, 2 });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PricePerUnit", "PriceUnit" },
                values: new object[] { 0.4m, 2 });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "PricePerUnit", "PriceUnit" },
                values: new object[] { 0.6m, 2 });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "PricePerUnit", "PriceUnit" },
                values: new object[] { 12m, 7 });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "PricePerUnit", "PriceUnit" },
                values: new object[] { 0.5m, 2 });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "PricePerUnit", "PriceUnit" },
                values: new object[] { 1.5m, 7 });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "PricePerUnit", "PriceUnit" },
                values: new object[] { 14m, 6 });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "PricePerUnit", "PriceUnit" },
                values: new object[] { 2m, 6 });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "PricePerUnit", "PriceUnit" },
                values: new object[] { 3.5m, 6 });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "PricePerUnit", "PriceUnit" },
                values: new object[] { 1.2m, 2 });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "PricePerUnit", "PriceUnit" },
                values: new object[] { 2m, 6 });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "PricePerUnit", "PriceUnit" },
                values: new object[] { 4m, 6 });

            migrationBuilder.UpdateData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "PricePerUnit", "PriceUnit" },
                values: new object[] { 10m, 6 });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_UserId",
                table: "Purchases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeBookmarks_RecipeId",
                table: "RecipeBookmarks",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeBookmarks_UserId_RecipeId",
                table: "RecipeBookmarks",
                columns: new[] { "UserId", "RecipeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "RecipeBookmarks");

            migrationBuilder.DropColumn(
                name: "PricePerUnit",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "PriceUnit",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "CookedMeals");
        }
    }
}
