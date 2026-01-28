// API Configuration
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

// Generic API request handler
async function apiRequest<T>(
    endpoint: string,
    options: RequestInit = {}
): Promise<T> {
    const token = getAuthToken();

    const headers: HeadersInit = {
        'Content-Type': 'application/json',
        ...(token && { Authorization: `Bearer ${token}` }),
        ...options.headers,
    };

    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
        ...options,
        headers,
    });

    if (!response.ok) {
        if (response.status === 401) {
            // Token expired or invalid
            setAuthToken(null);
            throw new Error('Unauthorized - Please login again');
        }

        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.message || `API Error: ${response.status}`);
    }

    // Handle 204 No Content
    if (response.status === 204) {
        return {} as T;
    }

    return response.json();
}

// ============ Auth API ============
export interface LoginRequest {
    username: string;
    password: string;
}

export interface LoginResponse {
    token: string;
    user: {
        id: string;
        username: string;
        name: string;
        email: string;
        role: string;
        status: string;
    };
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
    user: {
        id: string;
        username: string;
        name: string;
        email: string;
        role: string;
        status: string;
    };
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

export interface UserListDto {
    id: string;
    username: string;
    name: string;
    email: string;
    role: string;
    status: string;
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

export const authApi = {
    register: async (data: RegisterRequest): Promise<RegisterResponse> => {
        const response = await apiRequest<RegisterResponse>('/auth/register', {
            method: 'POST',
            body: JSON.stringify(data),
        });
        setAuthToken(response.token);
        return response;
    },

    login: async (credentials: LoginRequest): Promise<LoginResponse> => {
        const response = await apiRequest<LoginResponse>('/auth/login', {
            method: 'POST',
            body: JSON.stringify(credentials),
        });
        setAuthToken(response.token);
        return response;
    },

    getCurrentUser: async (): Promise<UserDto> => {
        return apiRequest<UserDto>('/auth/me');
    },

    checkStoreSetupRequired: async (): Promise<{ storeSetupRequired: boolean }> => {
        return apiRequest<{ storeSetupRequired: boolean }>('/auth/store-setup-required');
    },

    logout: () => {
        setAuthToken(null);
    },
};

// ============ Users API ============
export const usersApi = {
    getAll: async (): Promise<UserListDto[]> => {
        return apiRequest<UserListDto[]>('/users');
    },

    create: async (data: CreateUserRequest): Promise<UserListDto> => {
        return apiRequest<UserListDto>('/users', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },

    update: async (id: string, data: UpdateUserRequest): Promise<UserListDto> => {
        return apiRequest<UserListDto>(`/users/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },

    delete: async (id: string): Promise<void> => {
        await apiRequest<void>(`/users/${id}`, {
            method: 'DELETE',
        });
    },
};

// ============ Store API ============
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
    address: string | null;
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
    address?: string;
    isActive?: boolean;
}

export const storeApi = {
    get: async (): Promise<StoreDto> => {
        return apiRequest<StoreDto>('/store');
    },

    getStatus: async (): Promise<{ isSetupComplete: boolean; storeSetupRequired: boolean }> => {
        return apiRequest<{ isSetupComplete: boolean; storeSetupRequired: boolean }>('/store/status');
    },

    setup: async (data: UpdateStoreRequest): Promise<StoreDto> => {
        return apiRequest<StoreDto>('/store/setup', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },

    update: async (data: UpdateStoreRequest): Promise<StoreDto> => {
        return apiRequest<StoreDto>('/store', {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },
};

// ============ Ingredients API ============
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

export const ingredientsApi = {
    getAll: async (): Promise<IngredientDto[]> => {
        return apiRequest<IngredientDto[]>('/ingredients');
    },

    getById: async (id: string): Promise<IngredientDto> => {
        return apiRequest<IngredientDto>(`/ingredients/${id}`);
    },

    create: async (data: CreateIngredientRequest): Promise<IngredientDto> => {
        return apiRequest<IngredientDto>('/ingredients', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },

    update: async (id: string, data: CreateIngredientRequest): Promise<IngredientDto> => {
        return apiRequest<IngredientDto>(`/ingredients/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },

    delete: async (id: string): Promise<void> => {
        await apiRequest<void>(`/ingredients/${id}`, {
            method: 'DELETE',
        });
    },
};

// ============ Recipes API ============
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

export const recipesApi = {
    getAll: async (): Promise<RecipeDto[]> => {
        return apiRequest<RecipeDto[]>('/recipes');
    },

    getById: async (id: string): Promise<RecipeDto> => {
        return apiRequest<RecipeDto>(`/recipes/${id}`);
    },

    create: async (data: CreateRecipeRequest): Promise<RecipeDto> => {
        return apiRequest<RecipeDto>('/recipes', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },

    update: async (id: string, data: CreateRecipeRequest): Promise<RecipeDto> => {
        return apiRequest<RecipeDto>(`/recipes/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },

    delete: async (id: string): Promise<void> => {
        await apiRequest<void>(`/recipes/${id}`, {
            method: 'DELETE',
        });
    },
};

// ============ Sales API ============
export interface SalesDataDto {
    id: string;
    date: string;
    recipeId: string;
    quantity: number;
}

export interface CreateSalesDataRequest {
    date: string;
    recipeId: string;
    quantity: number;
}

export interface SalesTrendDto {
    date: string;
    totalQuantity: number;
    recipeBreakdown: Array<{
        recipeId: string;
        recipeName: string;
        quantity: number;
    }>;
}

export interface IngredientUsageDto {
    ingredientId: string;
    ingredientName: string;
    quantity: number;
}

export const salesApi = {
    getAll: async (startDate?: string, endDate?: string): Promise<SalesDataDto[]> => {
        const params = new URLSearchParams();
        if (startDate) params.append('startDate', startDate);
        if (endDate) params.append('endDate', endDate);
        const queryString = params.toString();
        return apiRequest<SalesDataDto[]>(`/sales${queryString ? `?${queryString}` : ''}`);
    },

    getById: async (id: string): Promise<SalesDataDto> => {
        return apiRequest<SalesDataDto>(`/sales/${id}`);
    },

    getTrend: async (startDate: string, endDate: string): Promise<SalesTrendDto[]> => {
        return apiRequest<SalesTrendDto[]>(`/sales/trend?startDate=${startDate}&endDate=${endDate}`);
    },

    getIngredientUsageByDate: async (date: string): Promise<IngredientUsageDto[]> => {
        return apiRequest<IngredientUsageDto[]>(`/sales/ingredients/${date}`);
    },

    getRecipeSalesByDate: async (date: string): Promise<{ recipeId: string; recipeName: string; quantity: number }[]> => {
        return apiRequest<{ recipeId: string; recipeName: string; quantity: number }[]>(`/sales/recipes/${date}`);
    },

    create: async (data: CreateSalesDataRequest): Promise<SalesDataDto> => {
        return apiRequest<SalesDataDto>('/sales', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },

    update: async (id: string, data: CreateSalesDataRequest): Promise<SalesDataDto> => {
        return apiRequest<SalesDataDto>(`/sales/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },

    delete: async (id: string): Promise<void> => {
        await apiRequest<void>(`/sales/${id}`, {
            method: 'DELETE',
        });
    },
};

// ============ Wastage API ============
export interface WastageDataDto {
    id: string;
    date: string;
    ingredientId: string;
    quantity: number;
}

export interface CreateWastageDataRequest {
    date: string;
    ingredientId: string;
    quantity: number;
}

export interface WastageTrendDto {
    date: string;
    totalQuantity: number;
    ingredientBreakdown: Array<{
        ingredientId: string;
        ingredientName: string;
        quantity: number;
    }>;
}

export const wastageApi = {
    getAll: async (startDate?: string, endDate?: string): Promise<WastageDataDto[]> => {
        const params = new URLSearchParams();
        if (startDate) params.append('startDate', startDate);
        if (endDate) params.append('endDate', endDate);
        const queryString = params.toString();
        return apiRequest<WastageDataDto[]>(`/wastage${queryString ? `?${queryString}` : ''}`);
    },

    getById: async (id: string): Promise<WastageDataDto> => {
        return apiRequest<WastageDataDto>(`/wastage/${id}`);
    },

    getTrend: async (startDate: string, endDate: string): Promise<WastageTrendDto[]> => {
        return apiRequest<WastageTrendDto[]>(`/wastage/trend?startDate=${startDate}&endDate=${endDate}`);
    },

    create: async (data: CreateWastageDataRequest): Promise<WastageDataDto> => {
        return apiRequest<WastageDataDto>('/wastage', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },

    update: async (id: string, data: CreateWastageDataRequest): Promise<WastageDataDto> => {
        return apiRequest<WastageDataDto>(`/wastage/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },

    delete: async (id: string): Promise<void> => {
        await apiRequest<void>(`/wastage/${id}`, {
            method: 'DELETE',
        });
    },
};

// ============ Forecast API ============
export interface ForecastIngredientDto {
    ingredientId: string;
    ingredientName: string;
    unit: string;
    quantity: number;
}

export interface ForecastDto {
    id: string;
    date: string;
    recipeId: string;
    recipeName: string;
    predictedQuantity: number;
    ingredients: ForecastIngredientDto[];
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
    getForecast: async (days: number = 7): Promise<ForecastDto[]> => {
        return apiRequest<ForecastDto[]>(`/forecast?days=${days}`);
    },

    getSummary: async (days: number = 7): Promise<ForecastSummaryDto[]> => {
        return apiRequest<ForecastSummaryDto[]>(`/forecast/summary?days=${days}`);
    },

    getWeather: async (): Promise<WeatherDto> => {
        return apiRequest<WeatherDto>('/forecast/weather');
    },

    getHolidays: async (year: number): Promise<HolidayDto[]> => {
        return apiRequest<HolidayDto[]>(`/forecast/holidays/${year}`);
    },
};

// Export all APIs
export const api = {
    auth: authApi,
    users: usersApi,
    store: storeApi,
    ingredients: ingredientsApi,
    recipes: recipesApi,
    sales: salesApi,
    wastage: wastageApi,
    forecast: forecastApi,
};

export default api;
