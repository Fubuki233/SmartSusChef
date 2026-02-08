package com.smartsuschef.mobile.data

import android.content.Context
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey
import androidx.test.core.app.ApplicationProvider
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.smartsuschef.mobile.util.Constants
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.test.runTest
import org.junit.After
import org.junit.Before
import org.junit.Test
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.runner.RunWith

/**
 * Instrumentation tests for TokenManager using EncryptedSharedPreferences.
 * These tests will run on an Android device or emulator.
 */
@ExperimentalCoroutinesApi
@RunWith(AndroidJUnit4::class)
class TokenManagerTest {

    private lateinit var tokenManager: TokenManager
    private lateinit var context: Context

    // EncryptedSharedPreferences uses a filename, which we control in Constants.kt
    // We'll clear the actual production file before/after each test for isolation.

    @Before
    fun setup() {
        context = ApplicationProvider.getApplicationContext()

        // Create MasterKey using the new builder pattern
        val masterKey = MasterKey.Builder(context)
            .setKeyScheme(MasterKey.KeyScheme.AES256_GCM)
            .build()

        // Create EncryptedSharedPreferences using the new method
        val testPrefs = EncryptedSharedPreferences.create(
            context,
            Constants.SHARED_PREFS_FILE_NAME,
            masterKey,
            EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
            EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
        )
        testPrefs.edit().clear().apply() // Use apply() for asynchronous write

        tokenManager = TokenManager(context) // Initialize TokenManager
    }

    @After
    fun tearDown() {
        // Clear preferences after each test to prevent interference
        val masterKey = MasterKey.Builder(context)
            .setKeyScheme(MasterKey.KeyScheme.AES256_GCM)
            .build()
        val testPrefs = EncryptedSharedPreferences.create(
            context,
            Constants.SHARED_PREFS_FILE_NAME,
            masterKey,
            EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
            EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
        )
        testPrefs.edit().clear().apply()
    }

    @Test
    fun saveTokenShouldStoreTokenSecurely() = runTest {
        val testToken = "test_jwt_token_123"
        tokenManager.saveToken(testToken)
        assertEquals(testToken, tokenManager.getTokenFlow().first())
    }

    @Test
    fun getTokenShouldRetrieveStoredTokenSynchronously() = runTest {
        val testToken = "sync_jwt_token_456"
        tokenManager.saveToken(testToken)
        assertEquals(testToken, tokenManager.getToken())
    }

    @Test
    fun saveUserRoleShouldStoreUserRoleSecurely() {
        val testRole = "Manager"
        tokenManager.saveUserRole(testRole)
        assertEquals(testRole, tokenManager.getUserRole())
    }

    @Test
    fun getUserRoleShouldRetrieveStoredUserRoleSynchronously() {
        val testRole = "Employee"
        tokenManager.saveUserRole(testRole)
        assertEquals(testRole, tokenManager.getUserRole())
    }

    @Test
    fun clearSessionShouldRemoveTokenAndUserRole() = runTest {
        tokenManager.saveToken("some_token")
        tokenManager.saveUserRole("some_role")

        tokenManager.clearSession()

        assertNull(tokenManager.getTokenFlow().first())
        assertNull(tokenManager.getUserRole())
    }

    @Test
    fun getTokenShouldReturnNullWhenNoTokenIsStored() = runTest {
        assertNull(tokenManager.getToken())
    }

    @Test
    fun getUserRoleShouldReturnNullWhenNoRoleIsStored() {
        assertNull(tokenManager.getUserRole())
    }
}
