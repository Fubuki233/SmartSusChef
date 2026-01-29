// Core types for SmartSus Chef system
export interface User {
  id: string;
  username: string;
  role: 'employee' | 'manager';
  name: string;
  email?: string;
  status?: 'Active' | 'Inactive';
}

export interface StoreSettings {
  storeId: string;
  companyName: string;
  uen: string;
  storeName: string;
  outletLocation?: string;
  address: string;
  contactNumber: string;
  latitude?: number;
  longitude?: number;
  countryCode?: string;
}

export interface Ingredient {
  id: string;
  name: string;
  unit: string;
  carbonFootprint: number; // kg CO2 per unit
  supplierUnit?: string; // e.g., "10kg", "500ml"
  supplierUnitCost?: number; // Cost in S$ for the supplier unit
  totalUnitsInPackage?: number; // Total grams/ml in the package
}

export interface RecipeIngredient {
  ingredientId?: string;
  childRecipeId?: string;
  quantity: number; // quantity per plate in grams/ml
}

export interface Recipe {
  id: string;
  name: string;
  isSubRecipe?: boolean;
  isSellable?: boolean; // Whether the recipe can be sold
  unit?: string; // NEW: Unit of measurement for the recipe (e.g., 'L', 'kg', 'plate')
  ingredients: RecipeIngredient[];
  costPerPlate?: number; // Auto-calculated based on ingredients
  targetSellingPrice?: number; // Manager input
  wasteMultiplier?: number; // percentage (e.g., 5 for 5%)
}

export interface SalesData {
  id: string;
  date: string; // YYYY-MM-DD
  recipeId: string;
  quantity: number;
  createdAt?: string; // ISO timestamp
  modifiedAt?: string; // ISO timestamp
  editHistory?: EditHistory[]; // Added for audit trail
}

export interface WastageData {
  id: string;
  date: string; // YYYY-MM-DD
  recipeId?: string; // NEW: Support for recipe wastage
  ingredientId?: string; // Made optional since we now support recipes
  quantity: number;
  createdAt?: string; // ISO timestamp
  modifiedAt?: string; // ISO timestamp
  editHistory?: EditHistory[]; // Added for audit trail
}

export interface ForecastData {
  date: string; // YYYY-MM-DD
  recipeId: string;
  quantity: number;
}

export interface HolidayEvent {
  date: string;
  name: string;
}

export interface WeatherData {
  temperature: number;
  condition: string;
  humidity: number;
  description: string;
}

export interface EditHistory {
  timestamp: string;
  editedBy: string;
  reason: string;
  previousValue: number;
  newValue: number;
}