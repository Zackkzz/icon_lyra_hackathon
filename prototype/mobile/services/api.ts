// Typed API client for the Mealer backend.
// A bearer token (set by the auth layer) is attached to every request.

import Constants from "expo-constants";

const API_PORT = 5097;

// Resolve the backend base URL.
// On a physical device via Expo Go, `localhost` points at the phone, not the
// dev machine — so derive the dev machine's LAN IP from the Metro packager host.
// An explicit EXPO_PUBLIC_API_URL always wins (e.g. a deployed backend).
function resolveBaseUrl(): string {
  if (process.env.EXPO_PUBLIC_API_URL) return process.env.EXPO_PUBLIC_API_URL;

  const hostUri =
    Constants.expoConfig?.hostUri ??
    (Constants as any).expoGoConfig?.debuggerHost ??
    (Constants as any).manifest2?.extra?.expoGo?.developer?.host;
  const host = hostUri?.split(":")[0];
  if (host) return `http://${host}:${API_PORT}`;

  // Fallback to production backend when no explicit URL is set.
  return "https://mealer.sunwookim.dev";
}

const BASE_URL = resolveBaseUrl();

// ---- Domain types ----

export type Unit =
  | "Grams"
  | "Ml"
  | "Pieces"
  | "Cups"
  | "Tbsp"
  | "Tsp"
  | "Kilograms"
  | "Litres"
  | "Ounces"
  | "Pounds";

export const UNITS: Unit[] = [
  "Grams",
  "Kilograms",
  "Ml",
  "Litres",
  "Pieces",
  "Cups",
  "Tbsp",
  "Tsp",
  "Ounces",
  "Pounds",
];

export type Source = "Receipt" | "Manual";

export type MealType = "Breakfast" | "Lunch" | "Dinner" | "Snack";
export const MEAL_TYPES: MealType[] = ["Breakfast", "Lunch", "Dinner", "Snack"];

// ---- Auth ----

export interface AuthResponse {
  token: string;
  userId: number;
  email: string;
  displayName: string;
}

// ---- Pantry ----

export interface PantryItem {
  id: number;
  ingredientId: number;
  ingredientName: string;
  category: string;
  quantity: number;
  unit: Unit;
  addedAt: string;
  source: Source;
  unitPrice: number | null; // price per priceUnit
  priceUnit: Unit | null;
  lineValue: number | null; // approx value of this stock
}

export interface AddPantryRequest {
  ingredientId: number;
  quantity: number;
  unit: Unit;
  source?: Source;
}

// ---- Receipt scanning ----

export interface ParsedReceiptItem {
  rawName: string;
  ingredientId: number | null;
  suggestedName: string;
  category: string;
  quantity: number;
  unit: Unit;
  price: number;
}

export interface ConfirmReceiptItem {
  ingredientId: number | null;
  name: string;
  category: string;
  quantity: number;
  unit: Unit;
  price: number;
}

// ---- Recipes ----

export interface RecipeIngredient {
  ingredientId: number;
  ingredientName: string;
  quantity: number;
  unit: Unit;
  inFridge: boolean;
  fridgeQuantity: number;
  lineCost: number | null;
}

export interface Recipe {
  id: number;
  name: string;
  description: string;
  instructions: string;
  servings: number;
  prepTimeMinutes: number;
  imageUrl: string | null;
  isAiGenerated: boolean;
  isBookmarked: boolean;
  costPerServing: number | null;
  totalCost: number | null;
  pricedIngredients: number;
  ingredients: RecipeIngredient[];
}

export interface RecipeSummary {
  id: number;
  name: string;
  description: string;
  servings: number;
  prepTimeMinutes: number;
  imageUrl: string | null;
  isAiGenerated: boolean;
  isBookmarked: boolean;
  costPerServing: number | null;
  matchCount: number;
  totalIngredients: number;
  matchPercentage: number;
}

export interface RecipeGroups {
  bookmarked: RecipeSummary[];
  canCook: RecipeSummary[];
  more: RecipeSummary[];
}

// ---- Cooked meals ----

export interface CookedMeal {
  id: number;
  recipeId: number | null;
  recipeName: string;
  portions: number;
  portionsAvailable: number; // portions not yet placed on the calendar
  cost: number;
  cookedAt: string;
}

// ---- Meal plan (calendar) ----

export interface MealPlan {
  id: number;
  date: string;
  mealType: MealType;
  cookedMealId: number | null;
  recipeId: number | null;
  recipeName: string | null;
  costPerPortion: number;
}

export interface CreateMealPlanRequest {
  date: string;
  mealType: MealType;
  cookedMealId: number;
}

// ---- Spending ----

export interface PreppedMeal {
  recipeName: string;
  portions: number;
  cost: number;
  cookedAt: string;
}

export interface WeekSpending {
  weekStart: string;
  spent: number;
  purchaseCount: number;
  preppedCost: number;
  mealsPrepped: PreppedMeal[];
}

export interface SpendingResponse {
  totalSpent: number;
  weeks: WeekSpending[];
}

export type SpendingPeriod = "day" | "week" | "month";

export interface SpendingTrendPoint {
  startDate: string;
  label: string;
  spent: number;
  purchaseCount: number;
  isCurrent: boolean;
}

export interface SpendingTrendResponse {
  period: SpendingPeriod;
  currentPeriodLabel: string;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  currentSpent: number;
  previousSpent: number;
  changePercentage: number | null;
  visibleTotal: number;
  currentPurchaseCount: number;
  points: SpendingTrendPoint[];
}

// ---- Token + request plumbing ----

let authToken: string | null = null;
export function setAuthToken(token: string | null) {
  authToken = token;
}

export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string>),
  };
  if (authToken) headers.Authorization = `Bearer ${authToken}`;

  const res = await fetch(`${BASE_URL}${path}`, { ...options, headers });

  if (!res.ok) {
    let message = `Request failed (${res.status})`;
    try {
      const body = await res.json();
      message = body.error || body.title || body.message || message;
    } catch {
      // non-JSON error body
    }
    throw new ApiError(res.status, message);
  }

  if (res.status === 204) return undefined as T;
  const text = await res.text();
  return text ? (JSON.parse(text) as T) : (undefined as T);
}

export const api = {
  // auth
  register: (email: string, password: string, displayName?: string) =>
    request<AuthResponse>("/api/auth/register", {
      method: "POST",
      body: JSON.stringify({ email, password, displayName }),
    }),
  login: (email: string, password: string) =>
    request<AuthResponse>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password }),
    }),
  me: () => request<AuthResponse>("/api/auth/me"),

  // pantry
  getPantry: () => request<PantryItem[]>("/api/Fridge"),
  addToPantry: (body: AddPantryRequest) =>
    request<PantryItem>("/api/Fridge", { method: "POST", body: JSON.stringify(body) }),
  deletePantryItem: (id: number) =>
    request<void>(`/api/Fridge/${id}`, { method: "DELETE" }),
  setIngredientPrice: (ingredientId: number, pricePerUnit: number, priceUnit: Unit) =>
    request<void>(`/api/Fridge/ingredient-price/${ingredientId}`, {
      method: "PUT",
      body: JSON.stringify({ pricePerUnit, priceUnit }),
    }),
  scanReceipt: (imageBase64: string) =>
    request<{ items: ParsedReceiptItem[] }>("/api/Fridge/scan-receipt", {
      method: "POST",
      body: JSON.stringify({ imageBase64 }),
    }),
  confirmReceipt: (items: ConfirmReceiptItem[]) =>
    request<PantryItem[]>("/api/Fridge/confirm-receipt", {
      method: "POST",
      body: JSON.stringify({ items }),
    }),

  // recipes
  getRecipes: () => request<RecipeSummary[]>("/api/Recipes"),
  getRecipe: (id: number) => request<Recipe>(`/api/Recipes/${id}`),
  getRecipeGroups: () => request<RecipeGroups>("/api/Recipes/for-fridge"),
  generateRecipes: (count = 3) =>
    request<Recipe[]>("/api/Recipes/generate", {
      method: "POST",
      body: JSON.stringify({ count }),
    }),
  toggleBookmark: (id: number) =>
    request<{ bookmarked: boolean }>(`/api/Recipes/${id}/bookmark`, { method: "POST" }),

  // cooking + spending
  cookRecipe: (recipeId: number, portions: number) =>
    request<CookedMeal>("/api/CookedMeals", {
      method: "POST",
      body: JSON.stringify({ recipeId, portions }),
    }),
  getCookedMeals: () => request<CookedMeal[]>("/api/CookedMeals"),
  getSpending: () => request<SpendingResponse>("/api/Spending"),
  getSpendingTrend: (period: SpendingPeriod, utcOffsetMinutes: number) =>
    request<SpendingTrendResponse>(
      `/api/Spending/trend?period=${period}&utcOffsetMinutes=${utcOffsetMinutes}`
    ),

  // meal plan (calendar)
  getMealPlan: (week: string) => request<MealPlan[]>(`/api/MealPlan?week=${week}`),
  createMealPlan: (body: CreateMealPlanRequest) =>
    request<MealPlan>("/api/MealPlan", { method: "POST", body: JSON.stringify(body) }),
  deleteMealPlan: (id: number) =>
    request<void>(`/api/MealPlan/${id}`, { method: "DELETE" }),
};
