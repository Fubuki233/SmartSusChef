package com.smartsuschef.mobile.data

import android.content.Context
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import com.smartsuschef.mobile.util.Constants
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.runBlocking

// Extension property to create the DataStore instance
private val Context.dataStore by preferencesDataStore(name = Constants.DATASTORE_NAME)

class TokenManager(private val context: Context) {

    companion object {
        private val TOKEN_KEY = stringPreferencesKey(Constants.KEY_AUTH_TOKEN)
        private val USER_ROLE_KEY = stringPreferencesKey(Constants.KEY_USER_ROLE)
    }

    /**
     * Saves the JWT token to DataStore
     */
    suspend fun saveToken(token: String) {
        context.dataStore.edit { preferences ->
            preferences[TOKEN_KEY] = token
        }
    }

    /**
     * Retrieves the JWT token as a Flow (Reactive)
     */
    fun getTokenFlow(): Flow<String?> {
        return context.dataStore.data.map { preferences ->
            preferences[TOKEN_KEY]
        }
    }

    /**
     * Synchronous token retrieval for the Hilt AuthInterceptor
     */
    fun getToken(): String? = runBlocking {
        context.dataStore.data.map { preferences ->
            preferences[TOKEN_KEY]
        }.first()
    }

    /**
     * Saves the user role (Manager vs Employee) to handle UI restrictions
     */
    suspend fun saveUserRole(role: String) {
        context.dataStore.edit { preferences ->
            preferences[USER_ROLE_KEY] = role
        }
    }

    /**
     * Retrieves the user role synchronously
     */
    fun getUserRole(): String? = runBlocking {
        context.dataStore.data.map { preferences ->
            preferences[USER_ROLE_KEY]
        }.first()
    }

    /**
     * Clears all session data (Logout)
     */
    suspend fun clearSession() {
        context.dataStore.edit { preferences ->
            preferences.clear()
        }
    }
}