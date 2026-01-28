import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  ReactNode,
} from "react";
import {
  User,
  Ingredient,
  Recipe,
  SalesData,
  WastageData,
  ForecastData,
  HolidayEvent,
  WeatherData,
  StoreSettings,
} from "@/app/types";

import { api, UserListDto } from "@/app/services/api";

// --- Types for Context ---
interface AppContextType {
  user: User | null;
  storeSetupRequired: boolean;
  login: (username: string, password: string) => Promise<{ success: boolean; storeSetupRequired: boolean }>;
  register: (username: string, password: string, name: string, email: string) => Promise<{ success: boolean; error?: string }>;
  completeStoreSetup: () => void;
  logout: () => void;
  storeSettings: StoreSettings;
  updateStoreSettings: (settings: Partial<StoreSettings>) => Promise<void>;
  loadStoreSettings: () => Promise<void>;
  storeUsers: User[];
  loadStoreUsers: () => Promise<void>;
  addUser: (user: Omit<User, "id"> & { password: string }) => Promise<boolean>;
  updateUser: (id: string, user: Partial<User> & { password?: string }) => Promise<boolean>;
  deleteUser: (id: string) => Promise<boolean>;
  ingredients: Ingredient[];
  recipes: Recipe[];
  salesData: SalesData[];
  wastageData: WastageData[];
  forecastData: ForecastData[];
  addIngredient: (ingredient: Omit<Ingredient, "id">) => Promise<void>;
  updateIngredient: (id: string, ingredient: Partial<Ingredient>) => Promise<void>;
  deleteIngredient: (id: string) => Promise<void>;
  addRecipe: (recipe: Omit<Recipe, "id">) => Promise<void>;
  updateRecipe: (id: string, recipe: Partial<Recipe>) => Promise<void>;
  deleteRecipe: (id: string) => Promise<void>;
  addSalesData: (data: Omit<SalesData, "id">) => Promise<void>;
  updateSalesData: (id: string, data: Partial<SalesData>) => Promise<void>;
  deleteSalesData: (id: string) => Promise<void>;
  addWastageData: (data: Omit<WastageData, "id">) => Promise<void>;
  updateWastageData: (id: string, data: Partial<WastageData>) => Promise<void>;
  deleteWastageData: (id: string) => Promise<void>;
  updateForecastData: (data: ForecastData[]) => void;
  importSalesData: (data: SalesData[]) => Promise<void>;
  exportData: (type: "sales" | "wastage" | "forecast") => void;
  holidays: HolidayEvent[];
  weather: WeatherData | null;
  loading: boolean;
  error: string | null;
}

const AppContext = createContext<AppContextType | null>(null);

// ==========================================
// 1. DEFAULT MOCK DATA (FALLBACK for ML)
// ==========================================

const MOCK_HOLIDAYS: HolidayEvent[] = [
  { date: '2026-01-01', name: "New Year's Day", type: 'public' },
  { date: '2026-02-17', name: 'Chinese New Year', type: 'public' },
  { date: '2026-02-18', name: 'Chinese New Year', type: 'public' },
  { date: '2026-04-03', name: 'Good Friday', type: 'public' },
  { date: '2026-05-01', name: 'Labour Day', type: 'public' },
  { date: '2026-05-31', name: 'Vesak Day', type: 'public' },
  { date: '2026-06-15', name: 'Hari Raya Puasa', type: 'public' },
  { date: '2026-08-09', name: 'National Day', type: 'public' },
  { date: '2026-08-21', name: 'Hari Raya Haji', type: 'public' },
  { date: '2026-11-08', name: 'Deepavali', type: 'public' },
  { date: '2026-12-25', name: 'Christmas Day', type: 'public' },
];

const MOCK_WEATHER: WeatherData = {
  temperature: 30,
  condition: 'Partly Cloudy',
  humidity: 75,
  description: 'Warm with possible afternoon showers',
  feelsLike: 34
};

// ==========================================
// 2. DATA CONVERSION FUNCTIONS
// ==========================================

const convertIngredientsFromAPI = (apiData: any[]): Ingredient[] => {
  return apiData.map((ing) => ({
    id: ing.id,
    storeId: "SSC-2026-001",
    name: ing.name,
    unit: ing.unit,
    carbonFootprint: ing.carbonFootprint || 0,
  }));
};

const convertRecipesFromAPI = (apiData: any[]): Recipe[] => {
  return apiData.map((rec) => ({
    id: rec.id,
    storeId: "SSC-2026-001",
    name: rec.name,
    isSubRecipe: rec.isSubRecipe || false,
    isSellable: rec.isSellable !== false,
    ingredients: (rec.ingredients || []).map((ing: any) => ({
      ingredientId: ing.ingredientId || undefined,
      childRecipeId: ing.childRecipeId || undefined,
      quantity: ing.quantity,
    })),
  }));
};

const convertSalesFromAPI = (apiData: any[]): SalesData[] => {
  return apiData.map((sale) => ({
    id: sale.id,
    storeId: "SSC-2026-001",
    date: sale.date,
    recipeId: sale.recipeId,
    quantity: sale.quantity || 0,
  }));
};

const convertWastageFromAPI = (apiData: any[]): WastageData[] => {
  return apiData.map((waste) => ({
    id: waste.id,
    storeId: "SSC-2026-001",
    date: waste.date,
    ingredientId: waste.ingredientId,
    quantity: waste.quantity || 0,
  }));
};

const convertForecastFromAPI = (apiData: any[]): ForecastData[] => {
  return apiData.map((fc) => ({
    id: fc.id,
    storeId: "SSC-2026-001",
    date: fc.date,
    recipeId: fc.recipeId,
    predictedQuantity: fc.predictedQuantity || 0,
  }));
};

const convertUsersFromAPI = (apiData: UserListDto[]): User[] => {
  return apiData.map((u) => ({
    id: u.id,
    username: u.username,
    name: u.name,
    email: u.email,
    role: u.role as "manager" | "employee",
    status: u.status as "Active" | "Inactive",
  }));
};

// ==========================================
// 3. PROVIDER COMPONENT
// ==========================================

export function AppProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [storeSetupRequired, setStoreSetupRequired] = useState(false);
  const [data, setData] = useState<any>({
    storeSettings: {
      uen: "",
      storeName: "",
      outletLocation: "",
      address: "",
      contactNumber: "",
    },
    storeUsers: [],
    ingredients: [],
    recipes: [],
    salesData: [],
    wastageData: [],
    forecastData: [],
    holidays: MOCK_HOLIDAYS,
    weather: MOCK_WEATHER,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load user from localStorage on mount
  useEffect(() => {
    const stored = localStorage.getItem("smartsus-user");
    const storeSetup = localStorage.getItem("smartsus-store-setup-required");
    if (stored) {
      try {
        setUser(JSON.parse(stored));
        setStoreSetupRequired(storeSetup === "true");
      } catch (e) {
        console.error("Failed to load user from localStorage", e);
      }
    }
  }, []);

  // Load all data from API when user logs in (only if store setup is complete)
  useEffect(() => {
    if (user && !storeSetupRequired) {
      loadAllData();
    }
  }, [user, storeSetupRequired]);

  const loadAllData = async () => {
    setLoading(true);
    setError(null);
    try {
      const [ingredients, recipes, sales, wastage, forecast, holidays, weather, storeData] =
        await Promise.allSettled([
          api.ingredients.getAll(),
          api.recipes.getAll(),
          api.sales.getAll(),
          api.wastage.getAll(),
          api.forecast.getForecast(7),
          api.forecast.getHolidays(new Date().getFullYear()),
          api.forecast.getWeather(),
          api.store.get(),
        ]);

      setData((prev: any) => ({
        ...prev,
        ingredients: ingredients.status === "fulfilled" ? convertIngredientsFromAPI(ingredients.value) : [],
        recipes: recipes.status === "fulfilled" ? convertRecipesFromAPI(recipes.value) : [],
        salesData: sales.status === "fulfilled" ? convertSalesFromAPI(sales.value) : [],
        wastageData: wastage.status === "fulfilled" ? convertWastageFromAPI(wastage.value) : [],
        forecastData: forecast.status === "fulfilled" ? convertForecastFromAPI(forecast.value) : [],
        holidays: holidays.status === "fulfilled" ? (holidays.value || MOCK_HOLIDAYS) : MOCK_HOLIDAYS,
        weather: weather.status === "fulfilled" ? (weather.value || MOCK_WEATHER) : MOCK_WEATHER,
        storeSettings: storeData.status === "fulfilled" ? {
          uen: storeData.value.uen,
          storeName: storeData.value.storeName,
          outletLocation: storeData.value.outletLocation,
          address: storeData.value.address || "",
          contactNumber: storeData.value.contactNumber,
          companyName: storeData.value.companyName,
        } : prev.storeSettings,
      }));
    } catch (err) {
      console.error("Error loading data from API:", err);
      setError(err instanceof Error ? err.message : "Failed to load data");
    } finally {
      setLoading(false);
    }
  };

  // Load store settings
  const loadStoreSettings = async () => {
    try {
      const storeData = await api.store.get();
      setData((prev: any) => ({
        ...prev,
        storeSettings: {
          uen: storeData.uen,
          storeName: storeData.storeName,
          outletLocation: storeData.outletLocation,
          address: storeData.address || "",
          contactNumber: storeData.contactNumber,
          companyName: storeData.companyName,
        },
      }));
    } catch (err) {
      console.error("Failed to load store settings:", err);
    }
  };

  // Load store users
  const loadStoreUsers = async () => {
    try {
      const users = await api.users.getAll();
      setData((prev: any) => ({
        ...prev,
        storeUsers: convertUsersFromAPI(users),
      }));
    } catch (err) {
      console.error("Failed to load store users:", err);
    }
  };

  // Authentication methods
  const register = async (username: string, password: string, name: string, email: string): Promise<{ success: boolean; error?: string }> => {
    try {
      const response = await api.auth.register({ username, password, name, email });
      const userData: User = {
        id: response.user.id,
        username: response.user.username,
        role: response.user.role as "manager" | "employee",
        name: response.user.name,
        email: response.user.email,
        status: response.user.status as "Active" | "Inactive",
      };
      setUser(userData);
      setStoreSetupRequired(true);
      localStorage.setItem("smartsus-user", JSON.stringify(userData));
      localStorage.setItem("smartsus-store-setup-required", "true");
      return { success: true };
    } catch (err: any) {
      console.error("Registration failed:", err);
      return { success: false, error: err.message || "Registration failed" };
    }
  };

  const login = async (username: string, password: string): Promise<{ success: boolean; storeSetupRequired: boolean }> => {
    try {
      const response = await api.auth.login({ username, password });
      const userData: User = {
        id: response.user.id,
        username: response.user.username,
        role: response.user.role as "manager" | "employee",
        name: response.user.name,
        email: response.user.email || "",
        status: response.user.status as "Active" | "Inactive" || "Active",
      };
      setUser(userData);
      setStoreSetupRequired(response.storeSetupRequired);
      localStorage.setItem("smartsus-user", JSON.stringify(userData));
      localStorage.setItem("smartsus-store-setup-required", response.storeSetupRequired ? "true" : "false");
      return { success: true, storeSetupRequired: response.storeSetupRequired };
    } catch (err) {
      console.error("Login failed:", err);
      setError(err instanceof Error ? err.message : "Login failed");
      return { success: false, storeSetupRequired: false };
    }
  };

  const completeStoreSetup = () => {
    setStoreSetupRequired(false);
    localStorage.setItem("smartsus-store-setup-required", "false");
  };

  const logout = () => {
    setUser(null);
    setStoreSetupRequired(false);
    api.auth.logout();
    localStorage.removeItem("smartsus-user");
    localStorage.removeItem("smartsus-store-setup-required");
  };

  // Ingredient methods
  const addIngredient = async (ingredient: Omit<Ingredient, "id">) => {
    try {
      const created = await api.ingredients.create({
        name: ingredient.name,
        unit: ingredient.unit,
        carbonFootprint: ingredient.carbonFootprint,
      });
      setData((p: any) => ({
        ...p,
        ingredients: [...p.ingredients, convertIngredientsFromAPI([created])[0]],
      }));
    } catch (err) {
      console.error("Failed to add ingredient:", err);
      setError(err instanceof Error ? err.message : "Failed to add ingredient");
    }
  };

  const updateIngredient = async (id: string, ingredient: Partial<Ingredient>) => {
    try {
      const updated = await api.ingredients.update(id, {
        name: ingredient.name || "",
        unit: ingredient.unit || "",
        carbonFootprint: ingredient.carbonFootprint || 0,
      });
      setData((p: any) => ({
        ...p,
        ingredients: p.ingredients.map((ing: Ingredient) =>
          ing.id === id ? convertIngredientsFromAPI([updated])[0] : ing
        ),
      }));
    } catch (err) {
      console.error("Failed to update ingredient:", err);
      setError(err instanceof Error ? err.message : "Failed to update ingredient");
    }
  };

  const deleteIngredient = async (id: string) => {
    try {
      await api.ingredients.delete(id);
      setData((p: any) => ({
        ...p,
        ingredients: p.ingredients.filter((ing: Ingredient) => ing.id !== id),
      }));
    } catch (err) {
      console.error("Failed to delete ingredient:", err);
      setError(err instanceof Error ? err.message : "Failed to delete ingredient");
    }
  };

  // Recipe methods
  const addRecipe = async (recipe: Omit<Recipe, "id">) => {
    try {
      const created = await api.recipes.create({
        name: recipe.name,
        isSellable: !recipe.isSubRecipe, // If it's a sub-recipe, it's not directly sellable
        isSubRecipe: recipe.isSubRecipe || false,
        ingredients: recipe.ingredients.map((ing) => ({
          ingredientId: ing.ingredientId || undefined,
          childRecipeId: ing.childRecipeId || undefined,
          quantity: ing.quantity,
        })),
      });
      setData((p: any) => ({
        ...p,
        recipes: [...p.recipes, convertRecipesFromAPI([created])[0]],
      }));
    } catch (err) {
      console.error("Failed to add recipe:", err);
      setError(err instanceof Error ? err.message : "Failed to add recipe");
    }
  };

  const updateRecipe = async (id: string, recipe: Partial<Recipe>) => {
    try {
      const isSubRecipeValue = recipe.isSubRecipe === true;
      const updated = await api.recipes.update(id, {
        name: recipe.name || "",
        isSellable: !isSubRecipeValue,
        isSubRecipe: isSubRecipeValue,
        ingredients: (recipe.ingredients || []).map((ing) => ({
          ingredientId: ing.ingredientId || undefined,
          childRecipeId: ing.childRecipeId || undefined,
          quantity: ing.quantity,
        })),
      });
      setData((p: any) => ({
        ...p,
        recipes: p.recipes.map((rec: Recipe) =>
          rec.id === id ? convertRecipesFromAPI([updated])[0] : rec
        ),
      }));
    } catch (err) {
      console.error("Failed to update recipe:", err);
      setError(err instanceof Error ? err.message : "Failed to update recipe");
    }
  };

  const deleteRecipe = async (id: string) => {
    try {
      await api.recipes.delete(id);
      setData((p: any) => ({
        ...p,
        recipes: p.recipes.filter((rec: Recipe) => rec.id !== id),
      }));
    } catch (err) {
      console.error("Failed to delete recipe:", err);
      setError(err instanceof Error ? err.message : "Failed to delete recipe");
    }
  };

  // Sales data methods
  const addSalesData = async (salesData: Omit<SalesData, "id">) => {
    try {
      const created = await api.sales.create({
        date: salesData.date,
        recipeId: salesData.recipeId,
        quantity: salesData.quantity,
      });
      setData((p: any) => ({
        ...p,
        salesData: [...p.salesData, convertSalesFromAPI([created])[0]],
      }));
    } catch (err) {
      console.error("Failed to add sales data:", err);
      setError(err instanceof Error ? err.message : "Failed to add sales data");
    }
  };

  const updateSalesData = async (id: string, salesData: Partial<SalesData>) => {
    try {
      const updated = await api.sales.update(id, {
        date: salesData.date || "",
        recipeId: salesData.recipeId || "",
        quantity: salesData.quantity || 0,
      });
      setData((p: any) => ({
        ...p,
        salesData: p.salesData.map((item: SalesData) =>
          item.id === id ? convertSalesFromAPI([updated])[0] : item
        ),
      }));
    } catch (err) {
      console.error("Failed to update sales data:", err);
      setError(err instanceof Error ? err.message : "Failed to update sales data");
    }
  };

  const deleteSalesData = async (id: string) => {
    try {
      await api.sales.delete(id);
      setData((p: any) => ({
        ...p,
        salesData: p.salesData.filter((item: SalesData) => item.id !== id),
      }));
    } catch (err) {
      console.error("Failed to delete sales data:", err);
      setError(err instanceof Error ? err.message : "Failed to delete sales data");
    }
  };

  // Wastage data methods
  const addWastageData = async (wastageData: Omit<WastageData, "id">) => {
    try {
      const created = await api.wastage.create({
        date: wastageData.date,
        ingredientId: wastageData.ingredientId,
        quantity: wastageData.quantity,
      });
      setData((p: any) => ({
        ...p,
        wastageData: [...p.wastageData, convertWastageFromAPI([created])[0]],
      }));
    } catch (err) {
      console.error("Failed to add wastage data:", err);
      setError(err instanceof Error ? err.message : "Failed to add wastage data");
    }
  };

  const updateWastageData = async (id: string, wastageData: Partial<WastageData>) => {
    try {
      const updated = await api.wastage.update(id, {
        date: wastageData.date || "",
        ingredientId: wastageData.ingredientId || "",
        quantity: wastageData.quantity || 0,
      });
      setData((p: any) => ({
        ...p,
        wastageData: p.wastageData.map((item: WastageData) =>
          item.id === id ? convertWastageFromAPI([updated])[0] : item
        ),
      }));
    } catch (err) {
      console.error("Failed to update wastage data:", err);
      setError(err instanceof Error ? err.message : "Failed to update wastage data");
    }
  };

  const deleteWastageData = async (id: string) => {
    try {
      await api.wastage.delete(id);
      setData((p: any) => ({
        ...p,
        wastageData: p.wastageData.filter((item: WastageData) => item.id !== id),
      }));
    } catch (err) {
      console.error("Failed to delete wastage data:", err);
      setError(err instanceof Error ? err.message : "Failed to delete wastage data");
    }
  };

  // Forecast and import/export methods
  const updateForecastData = (forecastData: ForecastData[]) => {
    setData((p: any) => ({ ...p, forecastData }));
  };

  const importSalesData = async (salesDataArray: SalesData[]) => {
    try {
      const created = await Promise.all(
        salesDataArray.map((sale) =>
          api.sales.create({
            date: sale.date,
            recipeId: sale.recipeId,
            quantitySold: sale.quantity,
          })
        )
      );
      setData((p: any) => ({
        ...p,
        salesData: [...p.salesData, ...convertSalesFromAPI(created)],
      }));
    } catch (err) {
      console.error("Failed to import sales data:", err);
      setError(err instanceof Error ? err.message : "Failed to import sales data");
    }
  };

  const exportData = () => {
    // Implementation for export
  };

  // Store settings and users
  const updateStoreSettings = async (settings: Partial<StoreSettings>) => {
    try {
      await api.store.update({
        storeName: settings.storeName,
        uen: settings.uen,
        outletLocation: settings.outletLocation,
        address: settings.address,
        contactNumber: settings.contactNumber,
        companyName: (settings as any).companyName,
      });
      setData((p: any) => ({
        ...p,
        storeSettings: { ...p.storeSettings, ...settings },
      }));
    } catch (err) {
      console.error("Failed to update store settings:", err);
      setError(err instanceof Error ? err.message : "Failed to update store settings");
    }
  };

  const addUser = async (userData: Omit<User, "id"> & { password: string }): Promise<boolean> => {
    try {
      const created = await api.users.create({
        username: userData.username,
        password: userData.password,
        name: userData.name,
        email: userData.email || "",
        role: userData.role,
      });
      setData((p: any) => ({
        ...p,
        storeUsers: [...p.storeUsers, {
          id: created.id,
          username: created.username,
          name: created.name,
          email: created.email,
          role: created.role as "manager" | "employee",
          status: created.status as "Active" | "Inactive",
        }],
      }));
      return true;
    } catch (err) {
      console.error("Failed to add user:", err);
      setError(err instanceof Error ? err.message : "Failed to add user");
      return false;
    }
  };

  const updateUser = async (id: string, userData: Partial<User> & { password?: string }): Promise<boolean> => {
    try {
      const updated = await api.users.update(id, {
        username: userData.username,
        password: userData.password,
        name: userData.name,
        email: userData.email,
        role: userData.role,
        status: userData.status,
      });
      setData((p: any) => ({
        ...p,
        storeUsers: p.storeUsers.map((u: User) =>
          u.id === id ? {
            id: updated.id,
            username: updated.username,
            name: updated.name,
            email: updated.email,
            role: updated.role as "manager" | "employee",
            status: updated.status as "Active" | "Inactive",
          } : u
        ),
      }));
      return true;
    } catch (err) {
      console.error("Failed to update user:", err);
      setError(err instanceof Error ? err.message : "Failed to update user");
      return false;
    }
  };

  const deleteUser = async (id: string): Promise<boolean> => {
    try {
      await api.users.delete(id);
      setData((p: any) => ({
        ...p,
        storeUsers: p.storeUsers.filter((u: User) => u.id !== id),
      }));
      return true;
    } catch (err) {
      console.error("Failed to delete user:", err);
      setError(err instanceof Error ? err.message : "Failed to delete user");
      return false;
    }
  };

  const value: AppContextType = {
    user,
    storeSetupRequired,
    login,
    register,
    completeStoreSetup,
    logout,
    storeSettings: data.storeSettings,
    updateStoreSettings,
    loadStoreSettings,
    storeUsers: data.storeUsers || [],
    loadStoreUsers,
    addUser,
    updateUser,
    deleteUser,
    ingredients: data.ingredients,
    recipes: data.recipes,
    salesData: data.salesData,
    wastageData: data.wastageData,
    forecastData: data.forecastData,
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
    holidays: data.holidays || [],
    weather: data.weather || null,
    loading,
    error,
  };

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>;
}

export function useApp() {
  const context = useContext(AppContext);
  if (!context) {
    throw new Error("useApp must be used within AppProvider");
  }
  return context;
}
