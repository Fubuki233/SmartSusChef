import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  ReactNode,
  useCallback,
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

import {
  authApi,
  usersApi,
  storeApi,
  ingredientsApi,
  recipesApi,
  salesApi,
  wastageApi,
  forecastApi,
  exportApi,
  setAuthToken,
  getAuthToken,
  UserDto,
  StoreDto,
  IngredientDto,
  RecipeDto,
  SalesDataDto,
  WastageDataDto,
  ForecastDto,
  HolidayDto,
  WeatherDto,
} from "@/app/services/api";

// --- Types for Context ---
interface AppContextType {
  user: User | null;
  loading: boolean;
  dataLoading: boolean;
  storeSetupRequired: boolean;
  login: (username: string, password: string) => Promise<boolean>;
  logout: () => void;
  register: (username: string, password: string, name: string, email: string) => Promise<{ success: boolean; storeSetupRequired: boolean; error?: string }>;
  updateProfile: (data: { name?: string; email?: string }) => Promise<void>;
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>;
  setupStore: (settings: Partial<StoreSettings>) => Promise<void>;
  storeSettings: StoreSettings;
  updateStoreSettings: (settings: Partial<StoreSettings>) => Promise<void>;
  storeUsers: User[];
  addUser: (user: Omit<User, "id"> & { password?: string }) => Promise<void>;
  updateUser: (id: string, user: Partial<User> & { password?: string }) => Promise<void>;
  deleteUser: (id: string) => Promise<void>;
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
  exportData: (type: "sales" | "wastage" | "forecast") => Promise<void>;
  holidays: HolidayEvent[];
  weather: WeatherData | null;
  refreshData: () => Promise<void>;
}

const AppContext = createContext<AppContextType | null>(null);

// ==========================================
// HELPER FUNCTIONS TO CONVERT API TYPES TO FRONTEND TYPES
// ==========================================

const mapUserDto = (dto: UserDto): User => ({
  id: dto.id,
  username: dto.username,
  name: dto.name,
  email: dto.email,
  role: dto.role as 'employee' | 'manager',
  status: dto.status as 'Active' | 'Inactive',
});

const mapStoreDto = (dto: StoreDto): StoreSettings => ({
  storeId: dto.id.toString(),
  companyName: dto.companyName || '',
  uen: dto.uen || '',
  storeName: dto.storeName || '',
  outletLocation: dto.outletLocation || '',
  address: dto.address || '',
  contactNumber: dto.contactNumber || '',
  latitude: dto.latitude,
  longitude: dto.longitude,
  countryCode: dto.countryCode || '',
});

const mapIngredientDto = (dto: IngredientDto): Ingredient => ({
  id: dto.id,
  name: dto.name,
  unit: dto.unit,
  carbonFootprint: dto.carbonFootprint,
});

const mapRecipeDto = (dto: RecipeDto): Recipe => ({
  id: dto.id,
  name: dto.name,
  isSubRecipe: dto.isSubRecipe,
  isSellable: dto.isSellable,
  ingredients: dto.ingredients.map(ing => ({
    ingredientId: ing.ingredientId,
    childRecipeId: ing.childRecipeId,
    quantity: ing.quantity,
  })),
});

const mapSalesDataDto = (dto: SalesDataDto): SalesData => ({
  id: dto.id,
  date: dto.date,
  recipeId: dto.recipeId,
  quantity: dto.quantity,
  createdAt: dto.createdAt,
  modifiedAt: dto.modifiedAt,
});

const mapWastageDataDto = (dto: WastageDataDto): WastageData => ({
  id: dto.id,
  date: dto.date,
  ingredientId: dto.ingredientId,
  recipeId: dto.recipeId,
  quantity: dto.quantity,
  updatedAt: dto.updatedAt,
});

const mapForecastDto = (dto: ForecastDto): ForecastData => ({
  date: dto.date,
  recipeId: dto.recipeId,
  quantity: dto.quantity,
});

const mapHolidayDto = (dto: HolidayDto): HolidayEvent => ({
  date: dto.date,
  name: dto.name,
});

const mapWeatherDto = (dto: WeatherDto | null): WeatherData | null => {
  if (!dto || dto.temperature === undefined) return null;
  return {
    temperature: dto.temperature,
    condition: dto.condition,
    humidity: dto.humidity,
    description: dto.description,
  };
};

// Default empty store settings
const defaultStoreSettings: StoreSettings = {
  storeId: '',
  companyName: '',
  uen: '',
  storeName: '',
  outletLocation: '',
  address: '',
  contactNumber: '',
  latitude: undefined,
  longitude: undefined,
  countryCode: '',
};

// ==========================================
// PROVIDER COMPONENT
// ==========================================
export function AppProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [dataLoading, setDataLoading] = useState(false);
  const [storeSetupRequired, setStoreSetupRequired] = useState(false);
  const [storeSettings, setStoreSettings] = useState<StoreSettings>(defaultStoreSettings);
  const [storeUsers, setStoreUsers] = useState<User[]>([]);
  const [ingredients, setIngredients] = useState<Ingredient[]>([]);
  const [recipes, setRecipes] = useState<Recipe[]>([]);
  const [salesData, setSalesData] = useState<SalesData[]>([]);
  const [wastageData, setWastageData] = useState<WastageData[]>([]);
  const [forecastData, setForecastData] = useState<ForecastData[]>([]);
  const [holidays, setHolidays] = useState<HolidayEvent[]>([]);
  const [weather, setWeather] = useState<WeatherData | null>(null);

  // ==========================================
  // LOAD ALL DATA
  // ==========================================
  const loadAllData = useCallback(async () => {
    setDataLoading(true);
    try {
      // Load all data in parallel
      const [
        ingredientsData,
        recipesData,
        salesDataResult,
        wastageDataResult,
        forecastDataResult,
        weatherData,
        holidaysData,
        storeData,
      ] = await Promise.all([
        ingredientsApi.getAll().catch(() => []),
        recipesApi.getAll().catch(() => []),
        salesApi.getAll().catch(() => []),
        wastageApi.getAll().catch(() => []),
        forecastApi.get(7, 7).catch(() => []), // Get 7 days future + 7 days past (including today)
        forecastApi.getWeather().catch(() => null),
        forecastApi.getHolidays(new Date().getFullYear()).catch(() => []),
        storeApi.get().catch(() => null),
      ]);

      setIngredients(ingredientsData.map(mapIngredientDto));
      setRecipes(recipesData.map(mapRecipeDto));
      setSalesData(salesDataResult.map(mapSalesDataDto));
      setWastageData(wastageDataResult.map(mapWastageDataDto));

      const mappedForecast = forecastDataResult.map(mapForecastDto);
      console.log('[AppContext] Raw forecast data from API:', forecastDataResult.length);
      console.log('[AppContext] Mapped forecast data:', mappedForecast.length);
      console.log('[AppContext] Sample forecast dates:', mappedForecast.slice(0, 5).map(f => f.date));
      setForecastData(mappedForecast);

      setWeather(mapWeatherDto(weatherData));
      setHolidays(holidaysData.map(mapHolidayDto));

      if (storeData) {
        setStoreSettings(mapStoreDto(storeData));
      }

      // Load users if manager
      if (user?.role === 'manager') {
        const usersData = await usersApi.getAll().catch(() => []);
        setStoreUsers(usersData.map(u => mapUserDto(u)));
      }
    } catch (error) {
      console.error('Failed to load data:', error);
    } finally {
      setDataLoading(false);
    }
  }, [user?.role]);

  // ==========================================
  // INITIALIZE APP
  // ==========================================
  useEffect(() => {
    const initializeApp = async () => {
      const token = getAuthToken();
      if (token) {
        try {
          const userData = await authApi.getCurrentUser();
          setUser(mapUserDto(userData));
        } catch (error) {
          console.error('Failed to get current user:', error);
          setAuthToken(null);
        }
      }
      setLoading(false);
    };

    initializeApp();
  }, []);

  // Load data when user logs in
  useEffect(() => {
    if (user) {
      loadAllData();
    }
  }, [user, loadAllData]);

  // ==========================================
  // AUTH FUNCTIONS
  // ==========================================
  const login = async (username: string, password: string): Promise<boolean> => {
    try {
      const response = await authApi.login({ username, password });
      setAuthToken(response.token);
      setUser(mapUserDto(response.user));
      setStoreSetupRequired(response.storeSetupRequired);
      return true;
    } catch (error) {
      console.error('Login failed:', error);
      return false;
    }
  };

  const logout = () => {
    setUser(null);
    setAuthToken(null);
    setStoreSetupRequired(false);
    setStoreSettings(defaultStoreSettings);
    setStoreUsers([]);
    setIngredients([]);
    setRecipes([]);
    setSalesData([]);
    setWastageData([]);
    setForecastData([]);
    setHolidays([]);
    setWeather(null);
  };

  const register = async (username: string, password: string, name: string, email: string): Promise<{ success: boolean; storeSetupRequired: boolean; error?: string }> => {
    try {
      const response = await authApi.register({ username, password, name, email });
      setAuthToken(response.token);
      setUser(mapUserDto(response.user));
      setStoreSetupRequired(response.storeSetupRequired);
      return { success: true, storeSetupRequired: response.storeSetupRequired };
    } catch (error) {
      console.error('Registration failed:', error);
      const errorMessage = error instanceof Error ? error.message : 'Registration failed';
      return { success: false, storeSetupRequired: false, error: errorMessage };
    }
  };

  const updateProfile = async (data: { name?: string; email?: string }) => {
    try {
      const updated = await authApi.updateProfile(data);
      setUser(mapUserDto(updated));
    } catch (error) {
      console.error('Failed to update profile:', error);
      throw error;
    }
  };

  const changePassword = async (currentPassword: string, newPassword: string) => {
    try {
      await authApi.changePassword({ currentPassword, newPassword });
    } catch (error) {
      console.error('Failed to change password:', error);
      throw error;
    }
  };

  // ==========================================
  // STORE SETTINGS FUNCTIONS
  // ==========================================
  const setupStore = async (settings: Partial<StoreSettings>) => {
    try {
      const updatedStore = await storeApi.setup({
        companyName: settings.companyName,
        uen: settings.uen,
        storeName: settings.storeName,
        outletLocation: settings.outletLocation,
        address: settings.address,
        contactNumber: settings.contactNumber,
        latitude: settings.latitude,
        longitude: settings.longitude,
        countryCode: settings.countryCode,
      });
      setStoreSettings(mapStoreDto(updatedStore));
      setStoreSetupRequired(false);
    } catch (error) {
      console.error('Failed to setup store:', error);
      throw error;
    }
  };

  const updateStoreSettings = async (settings: Partial<StoreSettings>) => {
    try {
      const updatedStore = await storeApi.update({
        companyName: settings.companyName,
        uen: settings.uen,
        storeName: settings.storeName,
        outletLocation: settings.outletLocation,
        address: settings.address,
        contactNumber: settings.contactNumber,
        latitude: settings.latitude,
        longitude: settings.longitude,
        countryCode: settings.countryCode,
      });
      setStoreSettings(mapStoreDto(updatedStore));
    } catch (error) {
      console.error('Failed to update store settings:', error);
      throw error;
    }
  };

  // ==========================================
  // USER MANAGEMENT FUNCTIONS
  // ==========================================
  const addUser = async (userData: Omit<User, "id"> & { password?: string }) => {
    try {
      if (!userData.password) {
        throw new Error('Password is required');
      }
      const newUser = await usersApi.create({
        username: userData.username,
        password: userData.password,
        name: userData.name,
        email: userData.email || '',
        role: userData.role,
      });
      setStoreUsers(prev => [...prev, mapUserDto(newUser)]);
    } catch (error) {
      console.error('Failed to add user:', error);
      throw error;
    }
  };

  const updateUser = async (id: string, userData: Partial<User> & { password?: string }) => {
    try {
      const updatedUser = await usersApi.update(id, {
        username: userData.username,
        password: userData.password,
        name: userData.name,
        email: userData.email,
        role: userData.role,
        status: userData.status,
      });
      setStoreUsers(prev => prev.map(u => u.id === id ? mapUserDto(updatedUser) : u));
    } catch (error) {
      console.error('Failed to update user:', error);
      throw error;
    }
  };

  const deleteUser = async (id: string) => {
    try {
      await usersApi.delete(id);
      setStoreUsers(prev => prev.filter(u => u.id !== id));
    } catch (error) {
      console.error('Failed to delete user:', error);
      throw error;
    }
  };

  // ==========================================
  // INGREDIENT FUNCTIONS
  // ==========================================
  const addIngredient = async (ingredientData: Omit<Ingredient, "id">) => {
    try {
      const newIngredient = await ingredientsApi.create({
        name: ingredientData.name,
        unit: ingredientData.unit,
        carbonFootprint: ingredientData.carbonFootprint,
      });
      setIngredients(prev => [...prev, mapIngredientDto(newIngredient)]);
    } catch (error) {
      console.error('Failed to add ingredient:', error);
      throw error;
    }
  };

  const updateIngredient = async (id: string, ingredientData: Partial<Ingredient>) => {
    try {
      const current = ingredients.find(i => i.id === id);
      if (!current) throw new Error('Ingredient not found');

      const updated = await ingredientsApi.update(id, {
        name: ingredientData.name || current.name,
        unit: ingredientData.unit || current.unit,
        carbonFootprint: ingredientData.carbonFootprint ?? current.carbonFootprint,
      });
      setIngredients(prev => prev.map(i => i.id === id ? mapIngredientDto(updated) : i));
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

  // ==========================================
  // RECIPE FUNCTIONS
  // ==========================================
  const addRecipe = async (recipeData: Omit<Recipe, "id">) => {
    try {
      const newRecipe = await recipesApi.create({
        name: recipeData.name,
        isSellable: recipeData.isSellable ?? !recipeData.isSubRecipe,
        isSubRecipe: recipeData.isSubRecipe || false,
        ingredients: recipeData.ingredients.map(ing => ({
          ingredientId: ing.ingredientId,
          childRecipeId: ing.childRecipeId,
          quantity: ing.quantity,
        })),
      });
      setRecipes(prev => [...prev, mapRecipeDto(newRecipe)]);
    } catch (error) {
      console.error('Failed to add recipe:', error);
      throw error;
    }
  };

  const updateRecipe = async (id: string, recipeData: Partial<Recipe>) => {
    try {
      const current = recipes.find(r => r.id === id);
      if (!current) throw new Error('Recipe not found');

      const updated = await recipesApi.update(id, {
        name: recipeData.name || current.name,
        isSellable: recipeData.isSellable ?? current.isSellable ?? !current.isSubRecipe,
        isSubRecipe: recipeData.isSubRecipe ?? current.isSubRecipe ?? false,
        ingredients: (recipeData.ingredients || current.ingredients).map(ing => ({
          ingredientId: ing.ingredientId,
          childRecipeId: ing.childRecipeId,
          quantity: ing.quantity,
        })),
      });
      setRecipes(prev => prev.map(r => r.id === id ? mapRecipeDto(updated) : r));
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

  // ==========================================
  // SALES DATA FUNCTIONS
  // ==========================================
  const addSalesData = async (data: Omit<SalesData, "id">) => {
    try {
      const newSale = await salesApi.create({
        date: data.date,
        recipeId: data.recipeId,
        quantity: data.quantity,
      });
      setSalesData(prev => [...prev, mapSalesDataDto(newSale)]);
    } catch (error) {
      console.error('Failed to add sales data:', error);
      throw error;
    }
  };

  const updateSalesData = async (id: string, data: Partial<SalesData>) => {
    try {
      const current = salesData.find(s => s.id === id);
      if (!current) throw new Error('Sales data not found');

      // Only quantity can be updated
      const updated = await salesApi.update(id, {
        quantity: data.quantity ?? current.quantity,
      });
      setSalesData(prev => prev.map(s => s.id === id ? mapSalesDataDto(updated) : s));
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

  const importSalesData = async (data: SalesData[]) => {
    try {
      await salesApi.import({
        salesData: data.map(d => ({
          date: d.date,
          recipeId: d.recipeId,
          quantity: d.quantity,
        })),
      });
      // Refresh sales data after import
      const refreshed = await salesApi.getAll();
      setSalesData(refreshed.map(mapSalesDataDto));
    } catch (error) {
      console.error('Failed to import sales data:', error);
      throw error;
    }
  };

  // ==========================================
  // WASTAGE DATA FUNCTIONS
  // ==========================================
  const addWastageData = async (data: Omit<WastageData, "id">) => {
    try {
      const newWastage = await wastageApi.create({
        date: data.date,
        ingredientId: data.ingredientId,
        recipeId: data.recipeId,
        quantity: data.quantity,
      });
      setWastageData(prev => [...prev, mapWastageDataDto(newWastage)]);
    } catch (error) {
      console.error('Failed to add wastage data:', error);
      throw error;
    }
  };

  const updateWastageData = async (id: string, data: Partial<WastageData>) => {
    try {
      const current = wastageData.find(w => w.id === id);
      if (!current) throw new Error('Wastage data not found');

      const updated = await wastageApi.update(id, {
        date: data.date || current.date,
        ingredientId: data.ingredientId || current.ingredientId,
        recipeId: data.recipeId || current.recipeId,
        quantity: data.quantity ?? current.quantity,
      });
      setWastageData(prev => prev.map(w => w.id === id ? mapWastageDataDto(updated) : w));
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

  // ==========================================
  // FORECAST FUNCTIONS
  // ==========================================
  const updateForecastData = (data: ForecastData[]) => {
    setForecastData(data);
  };

  // ==========================================
  // EXPORT FUNCTION
  // ==========================================
  const exportData = async (type: "sales" | "wastage" | "forecast") => {
    try {
      let blob: Blob;
      let filename = '';

      if (type === 'sales') {
        blob = await exportApi.getSalesCsv();
        filename = 'sales_data.csv';
      } else if (type === 'wastage') {
        blob = await exportApi.getWastageCsv();
        filename = 'wastage_data.csv';
      } else if (type === 'forecast') {
        blob = await exportApi.getForecastCsv();
        filename = 'forecast_data.csv';
      } else {
        throw new Error('Invalid export type');
      }

      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = filename;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Failed to export data:', error);
      // You might want to show a toast notification here
    }
  };

  // ==========================================
  // REFRESH DATA FUNCTION
  // ==========================================
  const refreshData = async () => {
    await loadAllData();
  };

  const value: AppContextType = {
    user,
    loading,
    dataLoading,
    storeSetupRequired,
    login,
    logout,
    register,
    updateProfile,
    changePassword,
    setupStore,
    storeSettings,
    updateStoreSettings,
    storeUsers,
    addUser,
    updateUser,
    deleteUser,
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
