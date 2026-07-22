using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FridgeMealPlanner.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ingredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DensityGPerMl = table.Column<decimal>(type: "numeric(10,4)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Servings = table.Column<int>(type: "integer", nullable: false),
                    PrepTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WeekStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FridgeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IngredientId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Unit = table.Column<int>(type: "integer", nullable: false),
                    BestBeforeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FridgeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FridgeItems_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnitConversions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FromUnit = table.Column<int>(type: "integer", nullable: false),
                    ToUnit = table.Column<int>(type: "integer", nullable: false),
                    IngredientId = table.Column<int>(type: "integer", nullable: true),
                    Multiplier = table.Column<decimal>(type: "numeric(10,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitConversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitConversions_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MealPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    MealType = table.Column<int>(type: "integer", nullable: false),
                    RecipeId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealPlans_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecipeId = table.Column<int>(type: "integer", nullable: false),
                    IngredientId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Unit = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingListItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShoppingListId = table.Column<int>(type: "integer", nullable: false),
                    IngredientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Unit = table.Column<int>(type: "integer", nullable: false),
                    Purchased = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingListItems_ShoppingLists_ShoppingListId",
                        column: x => x.ShoppingListId,
                        principalTable: "ShoppingLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Ingredients",
                columns: new[] { "Id", "Category", "DensityGPerMl", "Name" },
                values: new object[,]
                {
                    { 1, "Meat", 1.05m, "Chicken Breast" },
                    { 2, "Grains", 0.85m, "Rice" },
                    { 3, "Vegetables", 0.74m, "Onion" },
                    { 4, "Vegetables", 1.10m, "Garlic" },
                    { 5, "Vegetables", 1.00m, "Tomato" },
                    { 6, "Oils", 0.92m, "Olive Oil" },
                    { 7, "Dairy", 1.03m, "Eggs" },
                    { 8, "Dairy", 1.03m, "Milk" },
                    { 9, "Dairy", 0.91m, "Butter" },
                    { 10, "Baking", 0.53m, "Flour" },
                    { 11, "Grains", 0.60m, "Pasta" },
                    { 12, "Vegetables", 0.51m, "Bell Pepper" },
                    { 13, "Vegetables", 0.64m, "Carrot" },
                    { 14, "Vegetables", 0.63m, "Broccoli" },
                    { 15, "Dairy", 1.10m, "Cheese" }
                });

            migrationBuilder.InsertData(
                table: "Recipes",
                columns: new[] { "Id", "Description", "ImageUrl", "Instructions", "Name", "PrepTimeMinutes", "Servings" },
                values: new object[,]
                {
                    { 1, "Quick and healthy chicken stir-fry with bell peppers and broccoli", null, "1. Cut chicken into strips. 2. Stir-fry chicken in oil. 3. Add sliced peppers and broccoli. 4. Add soy sauce and serve over rice.", "Chicken Stir-Fry", 25, 2 },
                    { 2, "Classic tomato pasta with garlic and fresh basil", null, "1. Cook pasta according to package. 2. Sauté garlic in olive oil. 3. Add chopped tomatoes and simmer. 4. Toss with pasta and top with cheese.", "Tomato Pasta", 20, 2 },
                    { 3, "Fluffy omelette with cheese and vegetables", null, "1. Beat eggs with milk. 2. Melt butter in pan. 3. Pour eggs, add cheese and veggies. 4. Fold and serve.", "Omelette", 10, 1 },
                    { 4, "Pan-seared chicken breast with garlic butter sauce", null, "1. Season chicken with salt and pepper. 2. Sear in butter. 3. Add minced garlic. 4. Baste and finish in pan. Serve with rice.", "Garlic Butter Chicken", 30, 2 },
                    { 5, "Hearty rice bowl with roasted vegetables and egg", null, "1. Cook rice. 2. Roast carrots and broccoli. 3. Top with fried egg. 4. Drizzle with olive oil.", "Vegetable Rice Bowl", 35, 2 }
                });

            migrationBuilder.InsertData(
                table: "UnitConversions",
                columns: new[] { "Id", "FromUnit", "IngredientId", "Multiplier", "ToUnit" },
                values: new object[,]
                {
                    { 1, 3, null, 240m, 1 },
                    { 2, 4, null, 15m, 1 },
                    { 3, 5, null, 5m, 1 },
                    { 4, 1, null, 0.0041666666666666666666666667m, 3 },
                    { 5, 1, null, 0.0666666666666666666666666667m, 4 },
                    { 6, 1, null, 0.2m, 5 },
                    { 7, 0, null, 1m, 0 },
                    { 8, 3, null, 16m, 4 },
                    { 9, 4, null, 3m, 5 },
                    { 10, 3, null, 48m, 5 },
                    { 17, 3, null, 200m, 0 },
                    { 18, 0, null, 0.005m, 3 },
                    { 19, 1, null, 1m, 0 },
                    { 20, 0, null, 1m, 1 }
                });

            migrationBuilder.InsertData(
                table: "RecipeIngredients",
                columns: new[] { "Id", "IngredientId", "Quantity", "RecipeId", "Unit" },
                values: new object[,]
                {
                    { 1, 1, 200m, 1, 0 },
                    { 2, 12, 1m, 1, 2 },
                    { 3, 14, 100m, 1, 0 },
                    { 4, 6, 2m, 1, 4 },
                    { 5, 2, 150m, 1, 0 },
                    { 6, 11, 200m, 2, 0 },
                    { 7, 5, 2m, 2, 2 },
                    { 8, 4, 2m, 2, 2 },
                    { 9, 6, 2m, 2, 4 },
                    { 10, 15, 30m, 2, 0 },
                    { 11, 7, 3m, 3, 2 },
                    { 12, 8, 2m, 3, 4 },
                    { 13, 9, 10m, 3, 0 },
                    { 14, 15, 20m, 3, 0 },
                    { 15, 1, 250m, 4, 0 },
                    { 16, 4, 4m, 4, 2 },
                    { 17, 9, 30m, 4, 0 },
                    { 18, 2, 150m, 4, 0 },
                    { 19, 2, 150m, 5, 0 },
                    { 20, 13, 100m, 5, 0 },
                    { 21, 14, 80m, 5, 0 },
                    { 22, 7, 1m, 5, 2 },
                    { 23, 6, 1m, 5, 4 }
                });

            migrationBuilder.InsertData(
                table: "UnitConversions",
                columns: new[] { "Id", "FromUnit", "IngredientId", "Multiplier", "ToUnit" },
                values: new object[,]
                {
                    { 11, 0, 10, 0.0083333333333333333333333333m, 3 },
                    { 12, 3, 10, 120m, 0 },
                    { 13, 0, 2, 0.005m, 3 },
                    { 14, 3, 2, 200m, 0 },
                    { 15, 0, 9, 0.0714285714285714285714285714m, 4 },
                    { 16, 4, 9, 14m, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FridgeItems_IngredientId",
                table: "FridgeItems",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_RecipeId",
                table: "MealPlans",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_IngredientId",
                table: "RecipeIngredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId",
                table: "RecipeIngredients",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItems_ShoppingListId",
                table: "ShoppingListItems",
                column: "ShoppingListId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversions_IngredientId",
                table: "UnitConversions",
                column: "IngredientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FridgeItems");

            migrationBuilder.DropTable(
                name: "MealPlans");

            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropTable(
                name: "ShoppingListItems");

            migrationBuilder.DropTable(
                name: "UnitConversions");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "ShoppingLists");

            migrationBuilder.DropTable(
                name: "Ingredients");
        }
    }
}
