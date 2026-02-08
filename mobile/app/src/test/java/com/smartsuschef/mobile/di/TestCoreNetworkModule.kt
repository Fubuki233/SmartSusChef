package com.smartsuschef.mobile.di

import android.content.Context
import com.google.gson.Gson
import com.smartsuschef.mobile.data.TokenManager
import dagger.Module
import dagger.Provides
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import dagger.hilt.testing.TestInstallIn
import io.mockk.mockk
import javax.inject.Singleton

@Module
@TestInstallIn(
    components = [SingletonComponent::class],
    replaces = [CoreNetworkModule::class]
)
object TestCoreNetworkModule {

    @Provides
    @Singleton
    fun provideBaseUrl(): String = "http://localhost:5001/api/"

    @Provides
    @Singleton
    fun provideGson(): Gson = Gson()

    @Provides
    @Singleton
    fun provideTokenManager(@ApplicationContext context: Context): TokenManager {
        return mockk(relaxed = true) // Use MockK for testing
    }
}