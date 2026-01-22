// Core types for SmartSus Chef system

export interface User {
  id: string;
  username: string;
  role: 'employee' | 'manager';
  name: string;
}

export interface Ingredient {
  id: string;
  name: string;
  unit: string;
  carbonFootprint: number; // kg CO2 per unit
}

export interface RecipeIngredient {
  ingredientId: string;
  quantity: number;
}

export interface Recipe {
  id: string;
  name: string;
  ingredients: RecipeIngredient[];
}

export interface SalesData {
  id: string;
  date: string; // YYYY-MM-DD
  recipeId: string;
  quantity: number;
}

export interface WastageData {
  id: string;
  date: string; // YYYY-MM-DD
  ingredientId: string;
  quantity: number;
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
