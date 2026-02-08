package com.smartsuschef.mobile.di

import com.smartsuschef.mobile.network.api.*
import dagger.Module
import dagger.Provides
import dagger.hilt.components.SingletonComponent
import dagger.hilt.testing.TestInstallIn // Ensure this import is present
import javax.inject.Singleton

/**
 * Hilt Module for providing MOCK ApiService implementations.
 * This module is only included in in test source sets.
 */
@Module
@TestInstallIn(
    components = [SingletonComponent::class],
    replaces = [ApiModule::class] // This tells Hilt to ignore the real ApiModule during tests
)
object ApiServiceModule {

    @Provides
    @Singleton
    fun provideAuthApiService(): AuthApiService {
        return MockAuthApiService()
    }

    @Provides
    @Singleton
    fun provideSalesApiService(): SalesApiService {
        return MockSalesApiService()
    }

    @Provides
    @Singleton
    fun provideWastageApiService(): WastageApiService {
        return MockWastageApiService()
    }

    @Provides
    @Singleton
    fun provideForecastApiService(): ForecastApiService {
        return MockForecastApiService()
    }

    @Provides
    @Singleton
    fun provideRecipeApiService(): RecipeApiService {
        return MockRecipeApiService()
    }

    @Provides
    @Singleton
    fun provideIngredientApiService(): IngredientApiService {
        return MockIngredientApiService()
    }

    @Provides
    @Singleton
    fun provideStoreApiService(): StoreApiService {
        return MockStoreApiService()
    }
}
