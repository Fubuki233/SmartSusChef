import React, { createContext, useContext, useState, useEffect, ReactNode, useCallback } from 'react';
import {
  User,
  Ingredient,
  Recipe,
  SalesData,
  WastageData,
  ForecastData,
  HolidayEvent,
  WeatherData,
} from '@/app/types';
import {
  authApi,
  ingredientsApi,
  recipesApi,
  salesApi,
  wastageApi,
  forecastApi,
  getAuthToken,
  setAuthToken,
} from '@/app/services/api';

interface AppContextType {
  // Authentication
  user: User | null;
  login: (username: string, password: string) => Promise<boolean>;
  logout: () => void;
  isLoading: boolean;

  // Data
  ingredients: Ingredient[];
  recipes: Recipe[];
  salesData: SalesData[];
  wastageData: WastageData[];
  forecastData: ForecastData[];

  // CRUD operations
  addIngredient: (ingredient: Omit<Ingredient, 'id'>) => Promise<void>;
  updateIngredient: (id: string, ingredient: Partial<Ingredient>) => Promise<void>;
  deleteIngredient: (id: string) => Promise<void>;

  addRecipe: (recipe: Omit<Recipe, 'id'>) => Promise<void>;
  updateRecipe: (id: string, recipe: Partial<Recipe>) => Promise<void>;
  deleteRecipe: (id: string) => Promise<void>;

  addSalesData: (data: Omit<SalesData, 'id'>) => Promise<void>;
  updateSalesData: (id: string, data: Partial<SalesData>) => Promise<void>;
  deleteSalesData: (id: string) => Promise<void>;

  addWastageData: (data: Omit<WastageData, 'id'>) => Promise<void>;
  updateWastageData: (id: string, data: Partial<WastageData>) => Promise<void>;
  deleteWastageData: (id: string) => Promise<void>;

  updateForecastData: (data: ForecastData[]) => void;

  // Import/Export
  importSalesData: (data: SalesData[]) => Promise<void>;
  exportData: (type: 'sales' | 'wastage' | 'forecast') => void;

  // External data
  holidays: HolidayEvent[];
  weather: WeatherData | null;

  // Refresh data
  refreshData: () => Promise<void>;
  refreshForecast: () => Promise<void>;
  refreshWeather: () => Promise<void>;
  refreshHolidays: (year?: number) => Promise<void>;
}

const AppContext = createContext<AppContextType | undefined>(undefined);

export function AppProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [ingredients, setIngredients] = useState<Ingredient[]>([]);
  const [recipes, setRecipes] = useState<Recipe[]>([]);
  const [salesData, setSalesData] = useState<SalesData[]>([]);
  const [wastageData, setWastageData] = useState<WastageData[]>([]);
  const [forecastData, setForecastData] = useState<ForecastData[]>([]);
  const [holidays, setHolidays] = useState<HolidayEvent[]>([]);
  const [weather, setWeather] = useState<WeatherData | null>(null);

  // Load all data from backend
  const loadAllData = useCallback(async () => {
    if (!getAuthToken()) return;

    try {
      // Load all data in parallel
      const [
        ingredientsData,
        recipesData,
        salesDataResult,
        wastageDataResult,
        forecastDataResult,
        weatherData,
      ] = await Promise.all([
        ingredientsApi.getAll().catch(() => []),
        recipesApi.getAll().catch(() => []),
        salesApi.getAll().catch(() => []),
        wastageApi.getAll().catch(() => []),
        forecastApi.getForecast(7).catch(() => []),
        forecastApi.getWeather().catch(() => null),
      ]);

      // Transform API data to match frontend types
      setIngredients(ingredientsData.map(i => ({
        id: i.id,
        name: i.name,
        unit: i.unit,
        carbonFootprint: i.carbonFootprint,
      })));

      setRecipes(recipesData.map(r => ({
        id: r.id,
        name: r.name,
        ingredients: r.ingredients.map(ri => ({
          ingredientId: ri.ingredientId,
          quantity: ri.quantity,
        })),
      })));

      setSalesData(salesDataResult.map(s => ({
        id: s.id,
        date: s.date,
        recipeId: s.recipeId,
        quantity: s.quantity,
      })));

      setWastageData(wastageDataResult.map(w => ({
        id: w.id,
        date: w.date,
        ingredientId: w.ingredientId,
        quantity: w.quantity,
      })));

      setForecastData(forecastDataResult.map(f => ({
        date: f.date,
        recipeId: f.recipeId,
        quantity: f.quantity,
      })));

      if (weatherData) {
        setWeather(weatherData);
      }

      // Load holidays for current year
      const currentYear = new Date().getFullYear();
      try {
        const holidaysData = await forecastApi.getHolidays(currentYear);
        setHolidays(holidaysData);
      } catch (e) {
        console.error('Failed to load holidays:', e);
      }

    } catch (error) {
      console.error('Failed to load data:', error);
    }
  }, []);

  // Check for existing session on mount
  useEffect(() => {
    const checkAuth = async () => {
      const token = getAuthToken();
      if (token) {
        try {
          const userData = await authApi.getCurrentUser();
          setUser({
            id: userData.id,
            username: userData.username,
            name: userData.name,
            role: userData.role as 'employee' | 'manager',
          });
          await loadAllData();
        } catch (error) {
          console.error('Session expired:', error);
          setAuthToken(null);
        }
      }
      setIsLoading(false);
    };

    checkAuth();
  }, [loadAllData]);

  // Login function
  const login = async (username: string, password: string): Promise<boolean> => {
    try {
      setIsLoading(true);
      const response = await authApi.login({ username, password });

      setUser({
        id: response.user.id,
        username: response.user.username,
        name: response.user.name,
        role: response.user.role as 'employee' | 'manager',
      });

      await loadAllData();
      return true;
    } catch (error) {
      console.error('Login failed:', error);
      return false;
    } finally {
      setIsLoading(false);
    }
  };

  // Logout function
  const logout = () => {
    authApi.logout();
    setUser(null);
    setIngredients([]);
    setRecipes([]);
    setSalesData([]);
    setWastageData([]);
    setForecastData([]);
  };

  // Refresh functions
  const refreshData = useCallback(async () => {
    setIsLoading(true);
    await loadAllData();
    setIsLoading(false);
  }, [loadAllData]);

  const refreshForecast = useCallback(async () => {
    try {
      const data = await forecastApi.getForecast(7);
      setForecastData(data.map(f => ({
        date: f.date,
        recipeId: f.recipeId,
        quantity: f.quantity,
      })));
    } catch (error) {
      console.error('Failed to refresh forecast:', error);
    }
  }, []);

  const refreshWeather = useCallback(async () => {
    try {
      const data = await forecastApi.getWeather();
      setWeather(data);
    } catch (error) {
      console.error('Failed to refresh weather:', error);
    }
  }, []);

  const refreshHolidays = useCallback(async (year?: number) => {
    try {
      const targetYear = year || new Date().getFullYear();
      const data = await forecastApi.getHolidays(targetYear);
      setHolidays(data);
    } catch (error) {
      console.error('Failed to refresh holidays:', error);
    }
  }, []);

  // CRUD Operations - Ingredients
  const addIngredient = async (ingredient: Omit<Ingredient, 'id'>) => {
    try {
      const newIngredient = await ingredientsApi.create({
        name: ingredient.name,
        unit: ingredient.unit,
        carbonFootprint: ingredient.carbonFootprint,
      });
      setIngredients(prev => [...prev, {
        id: newIngredient.id,
        name: newIngredient.name,
        unit: newIngredient.unit,
        carbonFootprint: newIngredient.carbonFootprint,
      }]);
    } catch (error) {
      console.error('Failed to add ingredient:', error);
      throw error;
    }
  };

  const updateIngredient = async (id: string, ingredient: Partial<Ingredient>) => {
    try {
      const existing = ingredients.find(i => i.id === id);
      if (!existing) throw new Error('Ingredient not found');

      const updated = await ingredientsApi.update(id, {
        name: ingredient.name || existing.name,
        unit: ingredient.unit || existing.unit,
        carbonFootprint: ingredient.carbonFootprint ?? existing.carbonFootprint,
      });

      setIngredients(prev => prev.map(i =>
        i.id === id ? {
          id: updated.id,
          name: updated.name,
          unit: updated.unit,
          carbonFootprint: updated.carbonFootprint,
        } : i
      ));
    } catch (error) {
      console.error('Failed to update ingredient:', error);
      throw error;
    }
  };

  const deleteIngredient = async (id: string) => {
    try {
      await ingredientsApi.delete(id);
      setIngredients(prev => prev.filter(i => i.id !== id));
    } catch (error) {
      console.error('Failed to delete ingredient:', error);
      throw error;
    }
  };

  // CRUD Operations - Recipes
  const addRecipe = async (recipe: Omit<Recipe, 'id'>) => {
    try {
      const newRecipe = await recipesApi.create({
        name: recipe.name,
        ingredients: recipe.ingredients.map(i => ({
          ingredientId: i.ingredientId,
          quantity: i.quantity,
        })),
      });

      setRecipes(prev => [...prev, {
        id: newRecipe.id,
        name: newRecipe.name,
        ingredients: newRecipe.ingredients.map(ri => ({
          ingredientId: ri.ingredientId,
          quantity: ri.quantity,
        })),
      }]);
    } catch (error) {
      console.error('Failed to add recipe:', error);
      throw error;
    }
  };

  const updateRecipe = async (id: string, recipe: Partial<Recipe>) => {
    try {
      const existing = recipes.find(r => r.id === id);
      if (!existing) throw new Error('Recipe not found');

      const updated = await recipesApi.update(id, {
        name: recipe.name || existing.name,
        ingredients: (recipe.ingredients || existing.ingredients).map(i => ({
          ingredientId: i.ingredientId,
          quantity: i.quantity,
        })),
      });

      setRecipes(prev => prev.map(r =>
        r.id === id ? {
          id: updated.id,
          name: updated.name,
          ingredients: updated.ingredients.map(ri => ({
            ingredientId: ri.ingredientId,
            quantity: ri.quantity,
          })),
        } : r
      ));
    } catch (error) {
      console.error('Failed to update recipe:', error);
      throw error;
    }
  };

  const deleteRecipe = async (id: string) => {
    try {
      await recipesApi.delete(id);
      setRecipes(prev => prev.filter(r => r.id !== id));
    } catch (error) {
      console.error('Failed to delete recipe:', error);
      throw error;
    }
  };

  // CRUD Operations - Sales Data
  const addSalesData = async (data: Omit<SalesData, 'id'>) => {
    try {
      const newSales = await salesApi.create({
        date: data.date,
        recipeId: data.recipeId,
        quantity: data.quantity,
      });

      setSalesData(prev => [...prev, {
        id: newSales.id,
        date: newSales.date,
        recipeId: newSales.recipeId,
        quantity: newSales.quantity,
      }]);
    } catch (error) {
      console.error('Failed to add sales data:', error);
      throw error;
    }
  };

  const updateSalesData = async (id: string, data: Partial<SalesData>) => {
    try {
      const existing = salesData.find(s => s.id === id);
      if (!existing) throw new Error('Sales data not found');

      const updated = await salesApi.update(id, {
        date: data.date || existing.date,
        recipeId: data.recipeId || existing.recipeId,
        quantity: data.quantity ?? existing.quantity,
      });

      setSalesData(prev => prev.map(s =>
        s.id === id ? {
          id: updated.id,
          date: updated.date,
          recipeId: updated.recipeId,
          quantity: updated.quantity,
        } : s
      ));
    } catch (error) {
      console.error('Failed to update sales data:', error);
      throw error;
    }
  };

  const deleteSalesData = async (id: string) => {
    try {
      await salesApi.delete(id);
      setSalesData(prev => prev.filter(s => s.id !== id));
    } catch (error) {
      console.error('Failed to delete sales data:', error);
      throw error;
    }
  };

  // CRUD Operations - Wastage Data
  const addWastageData = async (data: Omit<WastageData, 'id'>) => {
    try {
      const newWastage = await wastageApi.create({
        date: data.date,
        ingredientId: data.ingredientId,
        quantity: data.quantity,
      });

      setWastageData(prev => [...prev, {
        id: newWastage.id,
        date: newWastage.date,
        ingredientId: newWastage.ingredientId,
        quantity: newWastage.quantity,
      }]);
    } catch (error) {
      console.error('Failed to add wastage data:', error);
      throw error;
    }
  };

  const updateWastageData = async (id: string, data: Partial<WastageData>) => {
    try {
      const existing = wastageData.find(w => w.id === id);
      if (!existing) throw new Error('Wastage data not found');

      const updated = await wastageApi.update(id, {
        date: data.date || existing.date,
        ingredientId: data.ingredientId || existing.ingredientId,
        quantity: data.quantity ?? existing.quantity,
      });

      setWastageData(prev => prev.map(w =>
        w.id === id ? {
          id: updated.id,
          date: updated.date,
          ingredientId: updated.ingredientId,
          quantity: updated.quantity,
        } : w
      ));
    } catch (error) {
      console.error('Failed to update wastage data:', error);
      throw error;
    }
  };

  const deleteWastageData = async (id: string) => {
    try {
      await wastageApi.delete(id);
      setWastageData(prev => prev.filter(w => w.id !== id));
    } catch (error) {
      console.error('Failed to delete wastage data:', error);
      throw error;
    }
  };

  // Update forecast data locally (for editing)
  const updateForecastData = (data: ForecastData[]) => {
    setForecastData(data);
  };

  // Import sales data
  const importSalesData = async (data: SalesData[]) => {
    try {
      await salesApi.import(data.map(s => ({
        date: s.date,
        recipeId: s.recipeId,
        quantity: s.quantity,
      })));

      // Refresh sales data after import
      const updatedSales = await salesApi.getAll();
      setSalesData(updatedSales.map(s => ({
        id: s.id,
        date: s.date,
        recipeId: s.recipeId,
        quantity: s.quantity,
      })));
    } catch (error) {
      console.error('Failed to import sales data:', error);
      throw error;
    }
  };

  // Export data
  const exportData = (type: 'sales' | 'wastage' | 'forecast') => {
    let dataToExport;
    let filename;

    switch (type) {
      case 'sales':
        dataToExport = salesData;
        filename = 'sales-data.json';
        break;
      case 'wastage':
        dataToExport = wastageData;
        filename = 'wastage-data.json';
        break;
      case 'forecast':
        dataToExport = forecastData;
        filename = 'forecast-data.json';
        break;
    }

    const blob = new Blob([JSON.stringify(dataToExport, null, 2)], {
      type: 'application/json',
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <AppContext.Provider
      value={{
        user,
        login,
        logout,
        isLoading,
        ingredients,
        recipes,
        salesData,
        wastageData,
        forecastData,
        addIngredient,
        updateIngredient,
        deleteIngredient,
        addRecipe,
        updateRecipe,
        deleteRecipe,
        addSalesData,
        updateSalesData,
        deleteSalesData,
        addWastageData,
        updateWastageData,
        deleteWastageData,
        updateForecastData,
        importSalesData,
        exportData,
        holidays,
        weather,
        refreshData,
        refreshForecast,
        refreshWeather,
        refreshHolidays,
      }}
    >
      {children}
    </AppContext.Provider>
  );
}

export function useApp() {
  const context = useContext(AppContext);
  if (!context) {
    throw new Error('useApp must be used within AppProvider');
  }
  return context;
}