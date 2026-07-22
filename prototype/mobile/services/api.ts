const BASE_URL = process.env.EXPO_PUBLIC_API_URL || 'http://localhost:5000';

export interface FridgeItem {
  id: number;
  ingredientId: number;
  ingredientName: string;
  category: string;
  quantity: number;
  unit: Unit;
  bestBeforeDate: string;
  addedAt: string;
  source: string;
}

export type Unit = 'Grams' | 'Ml' | 'Pieces' | 'Cups' | 'Tbsp' | 'Tsp';
export type MealType = 'Breakfast' | 'Lunch' | 'Dinner' | 'Snack';

export interface Recipe {
  id: number;
  name: string;
  description: string;
  instructions: string;
  servings: number;
  prepTimeMinutes: number;
  imageUrl: string | null;
  ingredients: RecipeIngredient[];
}

export interface RecipeSummary {
  id: number;
  name: string;
  description: string;
  servings: number;
  prepTimeMinutes: number;
  imageUrl: string | null;
  matchCount: number;
  totalIngredients: number;
  matchPercentage: number;
}

export interface RecipeIngredient {
  ingredientId: number;
  ingredientName: string;
  quantity: number;
  unit: Unit;
}

export interface MealPlan {
  id: number;
  userId: string;
  date: string;
  mealType: MealType;
  recipeId: number | null;
  recipeName: string | null;
}

export interface ShoppingList {
  id: number;
  userId: string;
  weekStartDate: string;
  generatedAt: string;
  items: ShoppingListItem[];
}

export interface ShoppingListItem {
  id: number;
  ingredientName: string;
  quantity: number;
  unit: Unit;
  purchased: boolean;
}

export interface AddFridgeRequest {
  ingredientId: number;
  quantity: number;
  unit: Unit;
  bestBeforeDate: string;
  source?: string;
}

export interface CreateMealPlanRequest {
  userId: string;
  date: string;
  mealType: MealType;
  recipeId: number | null;
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  });
  if (!res.ok) throw new Error(`API error: ${res.status}`);
  return res.json();
}

export const api = {
  // Fridge
  getFridge: () => request<FridgeItem[]>('/api/fridge'),
  addToFridge: (data: AddFridgeRequest) =>
    request<FridgeItem>('/api/fridge', { method: 'POST', body: JSON.stringify(data) }),
  useFridgeItem: (id: number, quantity: number) =>
    request<FridgeItem>('/api/fridge/' + id + '/use?quantity=' + quantity, { method: 'PATCH' }),
  deleteFridgeItem: (id: number) =>
    request<void>('/api/fridge/' + id, { method: 'DELETE' }),

  // Recipes
  getRecipes: () => request<RecipeSummary[]>('/api/recipes'),
  getRecipe: (id: number) => request<Recipe>('/api/recipes/' + id),
  suggestRecipes: (ingredientIds: number[]) =>
    request<RecipeSummary[]>('/api/recipes/suggest?ingredients=' + ingredientIds.join(',')),

  // Meal Plan
  getMealPlan: (week: string) => request<MealPlan[]>('/api/mealplan?week=' + week),
  createMealPlan: (data: CreateMealPlanRequest) =>
    request<MealPlan>('/api/mealplan', { method: 'POST', body: JSON.stringify(data) }),
  updateMealPlan: (id: number, data: CreateMealPlanRequest) =>
    request<MealPlan>('/api/mealplan/' + id, { method: 'PUT', body: JSON.stringify(data) }),
  deleteMealPlan: (id: number) =>
    request<void>('/api/mealplan/' + id, { method: 'DELETE' }),

  // Shopping List
  generateShoppingList: (week: string, userId: string = 'default') =>
    request<ShoppingList>('/api/shopping-list/generate?week=' + week + '&userId=' + userId),

  // Conversions
  getConversions: () => request<any[]>('/api/conversions'),
  convert: (from: number, fromUnit: Unit, toUnit: Unit, ingredientId?: number) =>
    request<any>(`/api/conversions/convert?from=${from}&fromUnit=${fromUnit}&toUnit=${toUnit}&ingredientId=${ingredientId || ''}`),

  // Chat
  chat: (message: string) =>
    request<{ response: string }>('/api/chat', { method: 'POST', body: JSON.stringify({ message }) }),
};
