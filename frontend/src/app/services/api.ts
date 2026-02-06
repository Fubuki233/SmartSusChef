// API Service for SmartSusChef Frontend
// Connects to the backend API

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

// Token management
let authToken: string | null = null;

export const setAuthToken = (token: string | null) => {
  authToken = token;
  if (token) {
    localStorage.setItem('smartsus-token', token);
  } else {
    localStorage.removeItem('smartsus-token');
  }
};

export const getAuthToken = (): string | null => {
  if (!authToken) {
    authToken = localStorage.getItem('smartsus-token');
  }
  return authToken;
};

// Generic fetch wrapper with auth
async function fetchWithAuth<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const token = getAuthToken();

  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...options.headers,
  };

  if (token) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
    headers,
  });

  if (response.status === 401) {
    // Token expired or invalid
    setAuthToken(null);
    throw new Error('Unauthorized');
  }

  if (response.status === 204) {
    return {} as T;
  }

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Request failed' }));
    // Handle ASP.NET model validation errors ({ title, errors: { Field: ["msg"] } })
    if (error.errors && typeof error.errors === 'object') {
      const messages = Object.values(error.errors).flat().join(' ');
      throw new Error(messages || error.title || `HTTP error ${response.status}`);
    }
    throw new Error(error.message || error.title || `HTTP error ${response.status}`);
  }

  return response.json();
}

// Generic fetch wrapper for blob responses
async function fetchBlobWithAuth(
  endpoint: string,
  options: RequestInit = {}
): Promise<Blob> {
  const token = getAuthToken();

  const headers: HeadersInit = {
    ...options.headers,
  };

  if (token) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
    headers,
  });

  if (response.status === 401) {
    // Token expired or invalid
    setAuthToken(null);
    throw new Error('Unauthorized');
  }

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Request failed' }));
    if (error.errors && typeof error.errors === 'object') {
      const messages = Object.values(error.errors).flat().join(' ');
      throw new Error(messages || error.title || `HTTP error ${response.status}`);
    }
    throw new Error(error.message || error.title || `HTTP error ${response.status}`);
  }

  return response.blob();
}

// ==========================================
// AUTH API
// ==========================================
export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  user: UserDto;
  storeSetupRequired: boolean;
}

export interface RegisterRequest {
  username: string;
  password: string;
  name: string;
  email: string;
}

export interface RegisterResponse {
  token: string;
  user: UserDto;
  storeSetupRequired: boolean;
}

export interface UserDto {
  id: string;
  username: string;
  name: string;
  email: string;
  role: string;
  status: string;
}

export interface UserListDto extends UserDto {
  createdAt: string;
}

export interface CreateUserRequest {
  username: string;
  password: string;
  name: string;
  email: string;
  role: string;
}

export interface UpdateUserRequest {
  username?: string;
  password?: string;
  name?: string;
  email?: string;
  role?: string;
  status?: string;
}

export interface UpdateProfileRequest {
  name?: string;
  email?: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface ForgotPasswordRequest {
  emailOrUsername: string;
}

export interface ForgotPasswordResponse {
  temporaryPassword: string;
}

export const authApi = {
  login: (data: LoginRequest): Promise<LoginResponse> =>
    fetchWithAuth('/auth/login', { method: 'POST', body: JSON.stringify(data) }),

  register: (data: RegisterRequest): Promise<RegisterResponse> =>
    fetchWithAuth('/auth/register', { method: 'POST', body: JSON.stringify(data) }),

  getCurrentUser: (): Promise<UserDto> =>
    fetchWithAuth('/auth/me'),

  checkStoreSetupRequired: (): Promise<{ storeSetupRequired: boolean }> =>
    fetchWithAuth('/auth/store-setup-required'),

  updateProfile: (data: UpdateProfileRequest): Promise<UserDto> =>
    fetchWithAuth('/auth/profile', { method: 'PUT', body: JSON.stringify(data) }),

  changePassword: (data: ChangePasswordRequest): Promise<void> =>
    fetchWithAuth('/auth/password', { method: 'PUT', body: JSON.stringify(data) }),

  forgotPassword: (data: ForgotPasswordRequest): Promise<ForgotPasswordResponse> =>
    fetchWithAuth('/auth/forgot-password', { method: 'POST', body: JSON.stringify(data) }),
};

// ==========================================
// USERS API
// ==========================================
export const usersApi = {
  getAll: (): Promise<UserListDto[]> =>
    fetchWithAuth('/users'),

  create: (data: CreateUserRequest): Promise<UserListDto> =>
    fetchWithAuth('/users', { method: 'POST', body: JSON.stringify(data) }),

  update: (id: string, data: UpdateUserRequest): Promise<UserListDto> =>
    fetchWithAuth(`/users/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

  delete: (id: string): Promise<void> =>
    fetchWithAuth(`/users/${id}`, { method: 'DELETE' }),
};

// ==========================================
// STORE API
// ==========================================
export interface StoreDto {
  id: number;
  companyName: string;
  uen: string;
  storeName: string;
  outletLocation: string;
  contactNumber: string;
  openingDate: string;
  latitude: number;
  longitude: number;
  countryCode?: string;
  address?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateStoreRequest {
  companyName?: string;
  uen?: string;
  storeName?: string;
  outletLocation?: string;
  contactNumber?: string;
  openingDate?: string;
  latitude?: number;
  longitude?: number;
  countryCode?: string;
  address?: string;
  isActive?: boolean;
}

export const storeApi = {
  get: (): Promise<StoreDto> =>
    fetchWithAuth('/store'),

  getStatus: (): Promise<{ isSetupComplete: boolean; storeSetupRequired: boolean }> =>
    fetchWithAuth('/store/status'),

  setup: (data: UpdateStoreRequest): Promise<StoreDto> =>
    fetchWithAuth('/store/setup', { method: 'POST', body: JSON.stringify(data) }),

  update: (data: UpdateStoreRequest): Promise<StoreDto> =>
    fetchWithAuth('/store', { method: 'PUT', body: JSON.stringify(data) }),
};

// ==========================================
// INGREDIENTS API
// ==========================================
export interface IngredientDto {
  id: string;
  name: string;
  unit: string;
  carbonFootprint: number;
}

export interface CreateIngredientRequest {
  name: string;
  unit: string;
  carbonFootprint: number;
}

export interface UpdateIngredientRequest {
  name: string;
  unit: string;
  carbonFootprint: number;
}

export const ingredientsApi = {
  getAll: (): Promise<IngredientDto[]> =>
    fetchWithAuth('/ingredients'),

  getById: (id: string): Promise<IngredientDto> =>
    fetchWithAuth(`/ingredients/${id}`),

  create: (data: CreateIngredientRequest): Promise<IngredientDto> =>
    fetchWithAuth('/ingredients', { method: 'POST', body: JSON.stringify(data) }),

  update: (id: string, data: UpdateIngredientRequest): Promise<IngredientDto> =>
    fetchWithAuth(`/ingredients/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

  delete: (id: string): Promise<void> =>
    fetchWithAuth(`/ingredients/${id}`, { method: 'DELETE' }),
};

// ==========================================
// RECIPES API
// ==========================================
export interface RecipeIngredientDto {
  ingredientId?: string;
  childRecipeId?: string;
  displayName: string;
  unit: string;
  quantity: number;
}

export interface RecipeDto {
  id: string;
  name: string;
  isSellable: boolean;
  isSubRecipe: boolean;
  ingredients: RecipeIngredientDto[];
}

export interface CreateRecipeIngredientRequest {
  ingredientId?: string;
  childRecipeId?: string;
  quantity: number;
}

export interface CreateRecipeRequest {
  name: string;
  isSellable: boolean;
  isSubRecipe: boolean;
  ingredients: CreateRecipeIngredientRequest[];
}

export interface UpdateRecipeRequest {
  name: string;
  isSellable: boolean;
  isSubRecipe: boolean;
  ingredients: CreateRecipeIngredientRequest[];
}

export const recipesApi = {
  getAll: (): Promise<RecipeDto[]> =>
    fetchWithAuth('/recipes'),

  getById: (id: string): Promise<RecipeDto> =>
    fetchWithAuth(`/recipes/${id}`),

  create: (data: CreateRecipeRequest): Promise<RecipeDto> =>
    fetchWithAuth('/recipes', { method: 'POST', body: JSON.stringify(data) }),

  update: (id: string, data: UpdateRecipeRequest): Promise<RecipeDto> =>
    fetchWithAuth(`/recipes/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

  delete: (id: string): Promise<void> =>
    fetchWithAuth(`/recipes/${id}`, { method: 'DELETE' }),
};

// ==========================================
// SALES API
// ==========================================
export interface SalesDataDto {
  id: string;
  date: string;
  recipeId: string;
  recipeName: string;
  quantity: number;
  createdAt?: string;
  modifiedAt?: string;
}

export interface CreateSalesDataRequest {
  date: string;
  recipeId: string;
  quantity: number;
}

export interface UpdateSalesDataRequest {
  quantity: number;
}

export interface SalesTrendDto {
  date: string;
  totalQuantity: number;
  recipeBreakdown: RecipeSalesDto[];
}

export interface RecipeSalesDto {
  recipeId: string;
  recipeName: string;
  quantity: number;
}

export interface IngredientUsageDto {
  ingredientId: string;
  ingredientName: string;
  unit: string;
  quantity: number;
}

export interface ImportSalesDataRequest {
  salesData: CreateSalesDataRequest[];
}

// Import by dish name (auto-creates recipes)
export interface ImportSalesByNameItem {
  date: string;
  dishName: string;
  quantity: number;
}

export interface ImportSalesByNameRequest {
  salesData: ImportSalesByNameItem[];
  dateFormat?: string;  // .NET date format string (e.g. "M/d/yy", "yyyy-MM-dd")
}

export interface ImportSalesByNameResponse {
  message: string;
  imported: number;
  newDishesCreated: number;
  newDishes: string[];
}

export const salesApi = {
  getAll: (startDate?: string, endDate?: string): Promise<SalesDataDto[]> => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    const query = params.toString() ? `?${params.toString()}` : '';
    return fetchWithAuth(`/sales${query}`);
  },

  getById: (id: string): Promise<SalesDataDto> =>
    fetchWithAuth(`/sales/${id}`),

  getTrend: (startDate: string, endDate: string): Promise<SalesTrendDto[]> =>
    fetchWithAuth(`/sales/trend?startDate=${startDate}&endDate=${endDate}`),

  getIngredientUsageByDate: (date: string): Promise<IngredientUsageDto[]> =>
    fetchWithAuth(`/sales/ingredients/${date}`),

  getRecipeSalesByDate: (date: string): Promise<RecipeSalesDto[]> =>
    fetchWithAuth(`/sales/recipes/${date}`),

  create: (data: CreateSalesDataRequest): Promise<SalesDataDto> =>
    fetchWithAuth('/sales', { method: 'POST', body: JSON.stringify(data) }),

  import: (data: ImportSalesDataRequest): Promise<{ message: string; count: number }> =>
    fetchWithAuth('/sales/import', { method: 'POST', body: JSON.stringify(data) }),

  importByName: (data: ImportSalesByNameRequest): Promise<ImportSalesByNameResponse> =>
    fetchWithAuth('/sales/import-by-name', { method: 'POST', body: JSON.stringify(data) }),

  update: (id: string, data: UpdateSalesDataRequest): Promise<SalesDataDto> =>
    fetchWithAuth(`/sales/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

  delete: (id: string): Promise<void> =>
    fetchWithAuth(`/sales/${id}`, { method: 'DELETE' }),
};

// ==========================================
// WASTAGE API
// ==========================================
export interface WastageDataDto {
  id: string;
  date: string;
  ingredientId?: string;
  recipeId?: string;
  displayName: string;
  unit: string;
  quantity: number;
  carbonFootprint: number;
  updatedAt: string;
}

export interface CreateWastageDataRequest {
  date: string;
  ingredientId?: string;
  recipeId?: string;
  quantity: number;
}

export interface UpdateWastageDataRequest {
  date: string;
  ingredientId?: string;
  recipeId?: string;
  quantity: number;
}

export interface WastageTrendDto {
  date: string;
  totalQuantity: number;
  totalCarbonFootprint: number;
  itemBreakdown: ItemWastageDto[];
}

export interface ItemWastageDto {
  ingredientId?: string;
  recipeId?: string;
  displayName: string;
  unit: string;
  quantity: number;
  carbonFootprint: number;
}

export const wastageApi = {
  getAll: (startDate?: string, endDate?: string): Promise<WastageDataDto[]> => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    const query = params.toString() ? `?${params.toString()}` : '';
    return fetchWithAuth(`/wastage${query}`);
  },

  getById: (id: string): Promise<WastageDataDto> =>
    fetchWithAuth(`/wastage/${id}`),

  getTrend: (startDate: string, endDate: string): Promise<WastageTrendDto[]> =>
    fetchWithAuth(`/wastage/trend?startDate=${startDate}&endDate=${endDate}`),

  create: (data: CreateWastageDataRequest): Promise<WastageDataDto> =>
    fetchWithAuth('/wastage', { method: 'POST', body: JSON.stringify(data) }),

  update: (id: string, data: UpdateWastageDataRequest): Promise<WastageDataDto> =>
    fetchWithAuth(`/wastage/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

  delete: (id: string): Promise<void> =>
    fetchWithAuth(`/wastage/${id}`, { method: 'DELETE' }),
};

// ==========================================
// FORECAST API
// ==========================================
export interface ForecastIngredientDto {
  ingredientId: string;
  ingredientName: string;
  unit: string;
  quantity: number;
}

export interface ForecastDto {
  date: string;
  recipeId: string;
  recipeName: string;
  quantity: number;
  ingredients: ForecastIngredientDto[];
  confidence?: string; // "High" | "Medium" | "Low" — from ML backend
}

export interface ForecastSummaryDto {
  date: string;
  totalQuantity: number;
  changePercentage: number;
}

export interface WeatherDto {
  temperature: number;
  condition: string;
  humidity: number;
  description: string;
}

export interface HolidayDto {
  date: string;
  name: string;
}

export const forecastApi = {
  get: (days: number = 7, includePastDays: number = 0): Promise<ForecastDto[]> =>
    fetchWithAuth(`/forecast?days=${days}&includePastDays=${includePastDays}`),

  getSummary: (days: number = 7, includePastDays: number = 0): Promise<ForecastSummaryDto[]> =>
    fetchWithAuth(`/forecast/summary?days=${days}&includePastDays=${includePastDays}`),

  getWeather: (): Promise<WeatherDto | null> =>
    (fetchWithAuth<WeatherDto>('/forecast/weather').catch(() => null)),

  getHolidays: (year: number): Promise<HolidayDto[]> =>
    (fetchWithAuth<HolidayDto[]>(`/forecast/holidays/${year}`).catch(() => [])),
};

// ==========================================
// ML MODEL API
// ==========================================
export type MlModelStatus = 'ready' | 'training' | 'can_train' | 'insufficient_data' | 'unavailable';

export interface MlTrainingProgressDto {
  trained: number;
  failed: number;
  total: number;
  currentDish: string | null;
}

export interface MlStatusDto {
  storeId: number;
  hasModels: boolean;
  isTraining: boolean;
  dishes: string[] | null;
  daysAvailable: number | null;
  status: MlModelStatus;
  message: string;
  trainingProgress: MlTrainingProgressDto | null;
}

export interface MlTrainResponseDto {
  status: string;
  storeId: number;
  message: string | null;
}

export interface MlPredictResponseDto {
  storeId: number;
  status: string;
  message: string | null;
  daysAvailable: number | null;
  predictions: Record<string, unknown> | null;
}

export const mlApi = {
  /** Get ML model status for the current store */
  getStatus: (): Promise<MlStatusDto> =>
    fetchWithAuth('/ml/status'),

  /** Trigger model training for the current store */
  train: (): Promise<MlTrainResponseDto> =>
    fetchWithAuth('/ml/train', { method: 'POST' }),

  /** Trigger ML prediction for the current store */
  predict: (days: number = 7): Promise<MlPredictResponseDto> =>
    fetchWithAuth(`/ml/predict?days=${days}`, { method: 'POST' }),
};

// ==========================================
// EXPORT API
// ==========================================
export const exportApi = {
  getSalesCsv: (startDate?: string, endDate?: string): Promise<Blob> => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    const query = params.toString() ? `?${params.toString()}` : '';
    return fetchBlobWithAuth(`/export/sales/csv${query}`);
  },

  getWastageCsv: (startDate?: string, endDate?: string): Promise<Blob> => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    const query = params.toString() ? `?${params.toString()}` : '';
    return fetchBlobWithAuth(`/export/wastage/csv${query}`);
  },

  getForecastCsv: (days: number = 7): Promise<Blob> =>
    fetchBlobWithAuth(`/export/forecast/csv?days=${days}`),
};
