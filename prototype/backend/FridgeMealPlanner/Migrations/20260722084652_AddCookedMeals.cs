using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FridgeMealPlanner.Migrations
{
    /// <inheritdoc />
    public partial class AddCookedMeals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CookedMealId",
                table: "MealPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CookedMeals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RecipeId = table.Column<int>(type: "integer", nullable: true),
                    RecipeName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Portions = table.Column<int>(type: "integer", nullable: false),
                    CookedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CookedMeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CookedMeals_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_CookedMealId",
                table: "MealPlans",
                column: "CookedMealId");

            migrationBuilder.CreateIndex(
                name: "IX_CookedMeals_RecipeId",
                table: "CookedMeals",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_CookedMeals_UserId",
                table: "CookedMeals",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MealPlans_CookedMeals_CookedMealId",
                table: "MealPlans",
                column: "CookedMealId",
                principalTable: "CookedMeals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealPlans_CookedMeals_CookedMealId",
                table: "MealPlans");

            migrationBuilder.DropTable(
                name: "CookedMeals");

            migrationBuilder.DropIndex(
                name: "IX_MealPlans_CookedMealId",
                table: "MealPlans");

            migrationBuilder.DropColumn(
                name: "CookedMealId",
                table: "MealPlans");
        }
    }
}
