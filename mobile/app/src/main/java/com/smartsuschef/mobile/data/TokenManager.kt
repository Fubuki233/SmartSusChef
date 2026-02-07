package com.smartsuschef.mobile.data

import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import com.smartsuschef.mobile.util.Constants
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.runBlocking

class TokenManager(private val dataStore: DataStore<Preferences>) {

    // In-memory cache to avoid runBlocking on hot path
    @Volatile
    private var cachedToken: String? = null
    @Volatile
    private var cachedRole: String? = null

    companion object {
        val TOKEN_KEY = stringPreferencesKey(Constants.KEY_AUTH_TOKEN)
        val USER_ROLE_KEY = stringPreferencesKey(Constants.KEY_USER_ROLE)
    }

    /**
     * Saves the JWT token to DataStore and updates cache
     */
    suspend fun saveToken(token: String) {
        cachedToken = token
        dataStore.edit { preferences ->
            preferences[TOKEN_KEY] = token
        }
    }

    /**
     * Retrieves the JWT token as a Flow (Reactive)
     */
    fun getTokenFlow(): Flow<String?> {
        return dataStore.data.map { preferences ->
            preferences[TOKEN_KEY]
        }
    }

    /**
     * Synchronous token retrieval for the Hilt AuthInterceptor.
     * Uses in-memory cache to avoid blocking the main thread.
     * Falls back to runBlocking only on first access (cold start).
     */
    fun getToken(): String? {
        cachedToken?.let { return it }
        return runBlocking {
            dataStore.data.map { preferences ->
                preferences[TOKEN_KEY]
            }.first()
        }.also { cachedToken = it }
    }

    /**
     * Saves the user role (Manager vs Employee) to handle UI restrictions
     */
    suspend fun saveUserRole(role: String) {
        cachedRole = role
        dataStore.edit { preferences ->
            preferences[USER_ROLE_KEY] = role
        }
    }

    /**
     * Retrieves the user role synchronously (cached).
     */
    fun getUserRole(): String? {
        cachedRole?.let { return it }
        return runBlocking {
            dataStore.data.map { preferences ->
                preferences[USER_ROLE_KEY]
            }.first()
        }.also { cachedRole = it }
    }

    /**
     * Clears all session data (Logout)
     */
    suspend fun clearSession() {
        cachedToken = null
        cachedRole = null
        dataStore.edit { preferences ->
            preferences.clear()
        }
    }
}