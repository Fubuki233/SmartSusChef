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

    companion object {
        val TOKEN_KEY = stringPreferencesKey(Constants.KEY_AUTH_TOKEN)
        val USER_ROLE_KEY = stringPreferencesKey(Constants.KEY_USER_ROLE)
    }

    /**
     * Saves the JWT token to DataStore
     */
    suspend fun saveToken(token: String) {
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
     * Synchronous token retrieval for the Hilt AuthInterceptor
     */
    fun getToken(): String? = runBlocking {
        dataStore.data.map { preferences ->
            preferences[TOKEN_KEY]
        }.first()
    }

    /**
     * Saves the user role (Manager vs Employee) to handle UI restrictions
     */
    suspend fun saveUserRole(role: String) {
        dataStore.edit { preferences ->
            preferences[USER_ROLE_KEY] = role
        }
    }

    /**
     * Retrieves the user role synchronously
     */
    fun getUserRole(): String? = runBlocking {
        dataStore.data.map { preferences ->
            preferences[USER_ROLE_KEY]
        }.first()
    }

    /**
     * Clears all session data (Logout)
     */
    suspend fun clearSession() {
        dataStore.edit { preferences ->
            preferences.clear()
        }
    }
}