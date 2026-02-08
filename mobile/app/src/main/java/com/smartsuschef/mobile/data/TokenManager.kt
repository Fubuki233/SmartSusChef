package com.smartsuschef.mobile.data

import android.content.Context
import android.content.SharedPreferences
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey
import com.smartsuschef.mobile.util.Constants
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.runBlocking
// ...
class TokenManager(private val context: Context) {

    private val masterKey: MasterKey by lazy {
        val keyGenParameterSpec = KeyGenParameterSpec.Builder(
            MasterKey.DEFAULT_MASTER_KEY_ALIAS,
            KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT
        )
            .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
            .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
            .setKeySize(256)
            .build()
        MasterKey.Builder(context)
            .setKeyGenParameterSpec(keyGenParameterSpec)
            .build()
    }
    private val sharedPreferences: SharedPreferences by lazy {
        EncryptedSharedPreferences.create(
            context,
            Constants.SHARED_PREFS_FILE_NAME,
            masterKey,
            EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
            EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
        )
    }

    private val _authTokenFlow = MutableStateFlow<String?>(null)
    val authTokenFlow: Flow<String?> = _authTokenFlow.asStateFlow()

    init {
        // Load initial token on startup
        _authTokenFlow.value = sharedPreferences.getString(Constants.KEY_AUTH_TOKEN, null)
    }

    /**
     * Saves the JWT token to EncryptedSharedPreferences
     */
    fun saveToken(token: String) {
        sharedPreferences.edit().putString(Constants.KEY_AUTH_TOKEN, token).apply()
        _authTokenFlow.value = token
    }

    /**
     * Retrieves the JWT token as a Flow (Reactive)
     */
    // This is now based on MutableStateFlow for reactivity
    // The initial value is loaded in init block.
    // Subsequent changes via saveToken will update this flow.
    // The original getTokenFlow() functionality from DataStore (which watched for external changes)
    // is now handled by directly updating _authTokenFlow.value when saveToken is called internally.
    // For external preference changes (e.g., another app changing prefs directly), a more complex
    // SharedPreferences.OnSharedPreferenceChangeListener would be needed, but usually not for EncryptedSharedPreferences.
    fun getTokenFlow(): Flow<String?> {
        return authTokenFlow
    }

    /**
     * Synchronous token retrieval for the Hilt AuthInterceptor
     * Note: This still uses runBlocking to adapt the Flow to a synchronous call as required by the Interceptor.
     */
    fun getToken(): String? = runBlocking {
        _authTokenFlow.first()
    }

    /**
     * Saves the user role (Manager vs Employee) to EncryptedSharedPreferences
     */
    fun saveUserRole(role: String) {
        sharedPreferences.edit().putString(Constants.KEY_USER_ROLE, role).apply()
    }

    /**
     * Retrieves the user role synchronously
     */
    fun getUserRole(): String? {
        return sharedPreferences.getString(Constants.KEY_USER_ROLE, null)
    }

    /**
     * Clears all session data (Logout)
     */
    fun clearSession() {
        sharedPreferences.edit()
            .remove(Constants.KEY_AUTH_TOKEN)
            .remove(Constants.KEY_USER_ROLE)
            .apply()
        _authTokenFlow.value = null
    }
}