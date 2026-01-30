package com.smartsuschef.mobile.di

import android.content.Context
import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.smartsuschef.mobile.network.api.*
import com.smartsuschef.mobile.data.TokenManager
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit
import javax.inject.Singleton

/**
 * Network Module - Provides all networking dependencies via Hilt DI
 *
 * This module sets up:
 * 1. OkHttp client with auth interceptor and logging
 * 2. Retrofit instance with base URL and Gson converter
 * 3. All API service instances (Auth, Sales, Wastage, etc.)
 */
@Module
@InstallIn(SingletonComponent::class)
object NetworkModule {

    /**
     * Base URL for the API
     * Points to your teammate's server
     */
    private const val BASE_URL = "http://oversea.zyh111.icu:234/api/"

    /**
     * Provides Gson instance for JSON serialization/deserialization
     */
    @Provides
    @Singleton
    fun provideGson(): Gson {
        return GsonBuilder()
            .setLenient()
            .create()
    }

    /**
     * Provides TokenManager for storing/retrieving JWT tokens
     */
    @Provides
    @Singleton
    fun provideTokenManager(@ApplicationContext context: Context): TokenManager {
        return TokenManager(context)
    }

    /**
     * Provides HTTP Logging Interceptor for debugging API calls
     * Shows request/response details in Logcat
     */
    @Provides
    @Singleton
    fun provideLoggingInterceptor(): HttpLoggingInterceptor {
        return HttpLoggingInterceptor().apply {
            level = HttpLoggingInterceptor.Level.BODY // Change to NONE in production
        }
    }

    /**
     * Provides Auth Interceptor to add JWT token to all requests
     * Automatically adds "Authorization: Bearer {token}" header
     */
    @Provides
    @Singleton
    fun provideAuthInterceptor(tokenManager: TokenManager): Interceptor {
        return Interceptor { chain ->
            val originalRequest = chain.request()

            // Get token from TokenManager
            val token = tokenManager.getToken()

            // If token exists, add it to the request header
            val requestBuilder = originalRequest.newBuilder()
            if (!token.isNullOrEmpty()) {
                requestBuilder.addHeader("Authorization", "Bearer $token")
            }

            val request = requestBuilder.build()
            chain.proceed(request)
        }
    }

    /**
     * Provides OkHttpClient with interceptors and timeout configs
     */
    @Provides
    @Singleton
    fun provideOkHttpClient(
        loggingInterceptor: HttpLoggingInterceptor,
        authInterceptor: Interceptor
    ): OkHttpClient {
        return OkHttpClient.Builder()
            .addInterceptor(authInterceptor)      // Add auth token to requests
            .addInterceptor(loggingInterceptor)   // Log requests/responses
            .connectTimeout(30, TimeUnit.SECONDS) // Connection timeout
            .readTimeout(30, TimeUnit.SECONDS)    // Read timeout
            .writeTimeout(30, TimeUnit.SECONDS)   // Write timeout
            .build()
    }

    /**
     * Provides Retrofit instance
     */
    @Provides
    @Singleton
    fun provideRetrofit(
        okHttpClient: OkHttpClient,
        gson: Gson
    ): Retrofit {
        return Retrofit.Builder()
            .baseUrl(BASE_URL)
            .client(okHttpClient)
            .addConverterFactory(GsonConverterFactory.create(gson))
            .build()
    }

    // API SERVICE PROVIDERS

    /**
     * Provides AuthApiService for authentication endpoints
     */
    @Provides
    @Singleton
    fun provideAuthApiService(retrofit: Retrofit): AuthApiService {
        return retrofit.create(AuthApiService::class.java)
    }

    /**
     * Provides SalesApiService for sales data endpoints
     */
    @Provides
    @Singleton
    fun provideSalesApiService(retrofit: Retrofit): SalesApiService {
        return retrofit.create(SalesApiService::class.java)
    }

    /**
     * Provides WastageApiService for wastage data endpoints
     */
    @Provides
    @Singleton
    fun provideWastageApiService(retrofit: Retrofit): WastageApiService {
        return retrofit.create(WastageApiService::class.java)
    }

    /**
     * Provides ForecastApiService for forecast/weather/calendar endpoints
     */
    @Provides
    @Singleton
    fun provideForecastApiService(retrofit: Retrofit): ForecastApiService {
        return retrofit.create(ForecastApiService::class.java)
    }

    /**
     * Provides RecipeApiService for recipe management endpoints
     */
    @Provides
    @Singleton
    fun provideRecipeApiService(retrofit: Retrofit): RecipeApiService {
        return retrofit.create(RecipeApiService::class.java)
    }

    /**
     * Provides IngredientApiService for ingredient management endpoints
     */
    @Provides
    @Singleton
    fun provideIngredientApiService(retrofit: Retrofit): IngredientApiService {
        return retrofit.create(IngredientApiService::class.java)
    }

    /**
     * Provides StoreApiService for store information endpoints
     */
    @Provides
    @Singleton
    fun provideStoreApiService(retrofit: Retrofit): StoreApiService {
        return retrofit.create(StoreApiService::class.java)
    }
}