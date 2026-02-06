package com.smartsuschef.mobile.di

import com.smartsuschef.mobile.network.api.*
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import retrofit2.Retrofit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object ApiModule {

    @Provides
    @Singleton
    fun provideAuthApiService(retrofit: Retrofit): AuthApiService {
        return retrofit.create(AuthApiService::class.java)
    }

    @Provides
    @Singleton
    fun provideSalesApiService(retrofit: Retrofit): SalesApiService {
        return retrofit.create(SalesApiService::class.java)
    }

    @Provides
    @Singleton
    fun provideWastageApiService(retrofit: Retrofit): WastageApiService {
        return retrofit.create(WastageApiService::class.java)
    }

    @Provides
    @Singleton
    fun provideForecastApiService(retrofit: Retrofit): ForecastApiService {
        return retrofit.create(ForecastApiService::class.java)
    }

    @Provides
    @Singleton
    fun provideRecipeApiService(retrofit: Retrofit): RecipeApiService {
        return retrofit.create(RecipeApiService::class.java)
    }

    @Provides
    @Singleton
    fun provideIngredientApiService(retrofit: Retrofit): IngredientApiService {
        return retrofit.create(IngredientApiService::class.java)
    }

    @Provides
    @Singleton
    fun provideStoreApiService(retrofit: Retrofit): StoreApiService {
        return retrofit.create(StoreApiService::class.java)
    }
}
