using FridgeMealPlanner.Enums;
using FridgeMealPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace FridgeMealPlanner.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<FridgeItem> FridgeItems => Set<FridgeItem>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();
    public DbSet<CookedMeal> CookedMeals => Set<CookedMeal>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<RecipeBookmark> RecipeBookmarks => Set<RecipeBookmark>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<UnitConversion> UnitConversions => Set<UnitConversion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Fluent API relationships ----

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<FridgeItem>()
            .HasIndex(f => f.UserId);

        modelBuilder.Entity<FridgeItem>()
            .HasOne(f => f.Ingredient)
            .WithMany(i => i.FridgeItems)
            .HasForeignKey(f => f.IngredientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Recipe)
            .WithMany(r => r.RecipeIngredients)
            .HasForeignKey(ri => ri.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Ingredient)
            .WithMany(i => i.RecipeIngredients)
            .HasForeignKey(ri => ri.IngredientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MealPlan>()
            .HasOne(mp => mp.Recipe)
            .WithMany(r => r.MealPlans)
            .HasForeignKey(mp => mp.RecipeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Deleting a cooked meal removes its planned portions from the calendar.
        modelBuilder.Entity<MealPlan>()
            .HasOne(mp => mp.CookedMeal)
            .WithMany(cm => cm.MealPlans)
            .HasForeignKey(mp => mp.CookedMealId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CookedMeal>()
            .HasIndex(cm => cm.UserId);

        modelBuilder.Entity<CookedMeal>()
            .HasOne(cm => cm.Recipe)
            .WithMany()
            .HasForeignKey(cm => cm.RecipeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Purchase>()
            .HasIndex(p => p.UserId);

        modelBuilder.Entity<RecipeBookmark>()
            .HasIndex(b => new { b.UserId, b.RecipeId })
            .IsUnique();

        modelBuilder.Entity<RecipeBookmark>()
            .HasOne(b => b.Recipe)
            .WithMany()
            .HasForeignKey(b => b.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShoppingListItem>()
            .HasOne(sli => sli.ShoppingList)
            .WithMany(sl => sl.ShoppingListItems)
            .HasForeignKey(sli => sli.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UnitConversion>()
            .HasOne(uc => uc.Ingredient)
            .WithMany()
            .HasForeignKey(uc => uc.IngredientId)
            .OnDelete(DeleteBehavior.SetNull);

        // ---- Seed Data ----

        // 15 common ingredients, with seed prices (PriceUnit matches how recipes use them)
        var ingredients = new[]
        {
            new Ingredient { Id = 1, Name = "Chicken Breast", Category = "Meat", DensityGPerMl = 1.05m, PricePerUnit = 12m, PriceUnit = Unit.Kilograms },
            new Ingredient { Id = 2, Name = "Rice", Category = "Grains", DensityGPerMl = 0.85m, PricePerUnit = 2.5m, PriceUnit = Unit.Kilograms },
            new Ingredient { Id = 3, Name = "Onion", Category = "Vegetables", DensityGPerMl = 0.74m, PricePerUnit = 0.5m, PriceUnit = Unit.Pieces },
            new Ingredient { Id = 4, Name = "Garlic", Category = "Vegetables", DensityGPerMl = 1.10m, PricePerUnit = 0.4m, PriceUnit = Unit.Pieces },
            new Ingredient { Id = 5, Name = "Tomato", Category = "Vegetables", DensityGPerMl = 1.00m, PricePerUnit = 0.6m, PriceUnit = Unit.Pieces },
            new Ingredient { Id = 6, Name = "Olive Oil", Category = "Oils", DensityGPerMl = 0.92m, PricePerUnit = 12m, PriceUnit = Unit.Litres },
            new Ingredient { Id = 7, Name = "Eggs", Category = "Dairy", DensityGPerMl = 1.03m, PricePerUnit = 0.5m, PriceUnit = Unit.Pieces },
            new Ingredient { Id = 8, Name = "Milk", Category = "Dairy", DensityGPerMl = 1.03m, PricePerUnit = 1.5m, PriceUnit = Unit.Litres },
            new Ingredient { Id = 9, Name = "Butter", Category = "Dairy", DensityGPerMl = 0.91m, PricePerUnit = 14m, PriceUnit = Unit.Kilograms },
            new Ingredient { Id = 10, Name = "Flour", Category = "Baking", DensityGPerMl = 0.53m, PricePerUnit = 2m, PriceUnit = Unit.Kilograms },
            new Ingredient { Id = 11, Name = "Pasta", Category = "Grains", DensityGPerMl = 0.60m, PricePerUnit = 3.5m, PriceUnit = Unit.Kilograms },
            new Ingredient { Id = 12, Name = "Bell Pepper", Category = "Vegetables", DensityGPerMl = 0.51m, PricePerUnit = 1.2m, PriceUnit = Unit.Pieces },
            new Ingredient { Id = 13, Name = "Carrot", Category = "Vegetables", DensityGPerMl = 0.64m, PricePerUnit = 2m, PriceUnit = Unit.Kilograms },
            new Ingredient { Id = 14, Name = "Broccoli", Category = "Vegetables", DensityGPerMl = 0.63m, PricePerUnit = 4m, PriceUnit = Unit.Kilograms },
            new Ingredient { Id = 15, Name = "Cheese", Category = "Dairy", DensityGPerMl = 1.10m, PricePerUnit = 10m, PriceUnit = Unit.Kilograms }
        };

        modelBuilder.Entity<Ingredient>().HasData(ingredients);

        // 5 sample recipes
        var recipes = new[]
        {
            new Recipe { Id = 1, Name = "Chicken Stir-Fry", Description = "Quick and healthy chicken stir-fry with bell peppers and broccoli", Instructions = "1. Cut chicken into strips. 2. Stir-fry chicken in oil. 3. Add sliced peppers and broccoli. 4. Add soy sauce and serve over rice.", Servings = 2, PrepTimeMinutes = 25 },
            new Recipe { Id = 2, Name = "Tomato Pasta", Description = "Classic tomato pasta with garlic and fresh basil", Instructions = "1. Cook pasta according to package. 2. Sauté garlic in olive oil. 3. Add chopped tomatoes and simmer. 4. Toss with pasta and top with cheese.", Servings = 2, PrepTimeMinutes = 20 },
            new Recipe { Id = 3, Name = "Omelette", Description = "Fluffy omelette with cheese and vegetables", Instructions = "1. Beat eggs with milk. 2. Melt butter in pan. 3. Pour eggs, add cheese and veggies. 4. Fold and serve.", Servings = 1, PrepTimeMinutes = 10 },
            new Recipe { Id = 4, Name = "Garlic Butter Chicken", Description = "Pan-seared chicken breast with garlic butter sauce", Instructions = "1. Season chicken with salt and pepper. 2. Sear in butter. 3. Add minced garlic. 4. Baste and finish in pan. Serve with rice.", Servings = 2, PrepTimeMinutes = 30 },
            new Recipe { Id = 5, Name = "Vegetable Rice Bowl", Description = "Hearty rice bowl with roasted vegetables and egg", Instructions = "1. Cook rice. 2. Roast carrots and broccoli. 3. Top with fried egg. 4. Drizzle with olive oil.", Servings = 2, PrepTimeMinutes = 35 },
        };

        modelBuilder.Entity<Recipe>().HasData(recipes);

        // Recipe ingredients
        var recipeIngredients = new[]
        {
            // Chicken Stir-Fry (recipe 1)
            new RecipeIngredient { Id = 1, RecipeId = 1, IngredientId = 1, Quantity = 200, Unit = Unit.Grams },  // chicken
            new RecipeIngredient { Id = 2, RecipeId = 1, IngredientId = 12, Quantity = 1, Unit = Unit.Pieces },   // bell pepper
            new RecipeIngredient { Id = 3, RecipeId = 1, IngredientId = 14, Quantity = 100, Unit = Unit.Grams },  // broccoli
            new RecipeIngredient { Id = 4, RecipeId = 1, IngredientId = 6, Quantity = 2, Unit = Unit.Tbsp },      // olive oil
            new RecipeIngredient { Id = 5, RecipeId = 1, IngredientId = 2, Quantity = 150, Unit = Unit.Grams },   // rice

            // Tomato Pasta (recipe 2)
            new RecipeIngredient { Id = 6, RecipeId = 2, IngredientId = 11, Quantity = 200, Unit = Unit.Grams },  // pasta
            new RecipeIngredient { Id = 7, RecipeId = 2, IngredientId = 5, Quantity = 2, Unit = Unit.Pieces },    // tomatoes
            new RecipeIngredient { Id = 8, RecipeId = 2, IngredientId = 4, Quantity = 2, Unit = Unit.Pieces },    // garlic
            new RecipeIngredient { Id = 9, RecipeId = 2, IngredientId = 6, Quantity = 2, Unit = Unit.Tbsp },      // olive oil
            new RecipeIngredient { Id = 10, RecipeId = 2, IngredientId = 15, Quantity = 30, Unit = Unit.Grams },  // cheese

            // Omelette (recipe 3)
            new RecipeIngredient { Id = 11, RecipeId = 3, IngredientId = 7, Quantity = 3, Unit = Unit.Pieces },   // eggs
            new RecipeIngredient { Id = 12, RecipeId = 3, IngredientId = 8, Quantity = 2, Unit = Unit.Tbsp },     // milk
            new RecipeIngredient { Id = 13, RecipeId = 3, IngredientId = 9, Quantity = 10, Unit = Unit.Grams },   // butter
            new RecipeIngredient { Id = 14, RecipeId = 3, IngredientId = 15, Quantity = 20, Unit = Unit.Grams },  // cheese

            // Garlic Butter Chicken (recipe 4)
            new RecipeIngredient { Id = 15, RecipeId = 4, IngredientId = 1, Quantity = 250, Unit = Unit.Grams },  // chicken
            new RecipeIngredient { Id = 16, RecipeId = 4, IngredientId = 4, Quantity = 4, Unit = Unit.Pieces },   // garlic
            new RecipeIngredient { Id = 17, RecipeId = 4, IngredientId = 9, Quantity = 30, Unit = Unit.Grams },   // butter
            new RecipeIngredient { Id = 18, RecipeId = 4, IngredientId = 2, Quantity = 150, Unit = Unit.Grams },  // rice

            // Vegetable Rice Bowl (recipe 5)
            new RecipeIngredient { Id = 19, RecipeId = 5, IngredientId = 2, Quantity = 150, Unit = Unit.Grams },  // rice
            new RecipeIngredient { Id = 20, RecipeId = 5, IngredientId = 13, Quantity = 100, Unit = Unit.Grams }, // carrot
            new RecipeIngredient { Id = 21, RecipeId = 5, IngredientId = 14, Quantity = 80, Unit = Unit.Grams },  // broccoli
            new RecipeIngredient { Id = 22, RecipeId = 5, IngredientId = 7, Quantity = 1, Unit = Unit.Pieces },   // egg
            new RecipeIngredient { Id = 23, RecipeId = 5, IngredientId = 6, Quantity = 1, Unit = Unit.Tbsp },     // olive oil
        };

        modelBuilder.Entity<RecipeIngredient>().HasData(recipeIngredients);

        // 20 unit conversions
        var unitConversions = new[]
        {
            // Volume conversions (ingredient-independent)
            new UnitConversion { Id = 1, FromUnit = Unit.Cups, ToUnit = Unit.Ml, Multiplier = 240m },
            new UnitConversion { Id = 2, FromUnit = Unit.Tbsp, ToUnit = Unit.Ml, Multiplier = 15m },
            new UnitConversion { Id = 3, FromUnit = Unit.Tsp, ToUnit = Unit.Ml, Multiplier = 5m },
            new UnitConversion { Id = 4, FromUnit = Unit.Ml, ToUnit = Unit.Cups, Multiplier = 1m / 240m },
            new UnitConversion { Id = 5, FromUnit = Unit.Ml, ToUnit = Unit.Tbsp, Multiplier = 1m / 15m },
            new UnitConversion { Id = 6, FromUnit = Unit.Ml, ToUnit = Unit.Tsp, Multiplier = 1m / 5m },

            // Weight to weight
            new UnitConversion { Id = 7, FromUnit = Unit.Grams, ToUnit = Unit.Grams, Multiplier = 1m },

            // Volume cross conversions
            new UnitConversion { Id = 8, FromUnit = Unit.Cups, ToUnit = Unit.Tbsp, Multiplier = 16m },
            new UnitConversion { Id = 9, FromUnit = Unit.Tbsp, ToUnit = Unit.Tsp, Multiplier = 3m },
            new UnitConversion { Id = 10, FromUnit = Unit.Cups, ToUnit = Unit.Tsp, Multiplier = 48m },

            // Weight to volume for specific ingredients (using density)
            // Flour: 1 cup = 120g
            new UnitConversion { Id = 11, FromUnit = Unit.Grams, ToUnit = Unit.Cups, IngredientId = 10, Multiplier = 1m / 120m },
            new UnitConversion { Id = 12, FromUnit = Unit.Cups, ToUnit = Unit.Grams, IngredientId = 10, Multiplier = 120m },

            // Rice: 1 cup = 200g
            new UnitConversion { Id = 13, FromUnit = Unit.Grams, ToUnit = Unit.Cups, IngredientId = 2, Multiplier = 1m / 200m },
            new UnitConversion { Id = 14, FromUnit = Unit.Cups, ToUnit = Unit.Grams, IngredientId = 2, Multiplier = 200m },

            // Butter: 1 Tbsp = 14g
            new UnitConversion { Id = 15, FromUnit = Unit.Grams, ToUnit = Unit.Tbsp, IngredientId = 9, Multiplier = 1m / 14m },
            new UnitConversion { Id = 16, FromUnit = Unit.Tbsp, ToUnit = Unit.Grams, IngredientId = 9, Multiplier = 14m },

            // Sugar-like: generic cups to grams
            new UnitConversion { Id = 17, FromUnit = Unit.Cups, ToUnit = Unit.Grams, Multiplier = 200m },
            new UnitConversion { Id = 18, FromUnit = Unit.Grams, ToUnit = Unit.Cups, Multiplier = 1m / 200m },

            // Water-like: ml to grams
            new UnitConversion { Id = 19, FromUnit = Unit.Ml, ToUnit = Unit.Grams, Multiplier = 1m },
            new UnitConversion { Id = 20, FromUnit = Unit.Grams, ToUnit = Unit.Ml, Multiplier = 1m },
        };

        modelBuilder.Entity<UnitConversion>().HasData(unitConversions);
    }
}
