package com.smartsuschef.mobile.data

import androidx.datastore.preferences.core.PreferenceDataStoreFactory
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Rule
import org.junit.Test
import org.junit.rules.TemporaryFolder

/**
 * Unit tests for TokenManager.
 *
 * This version uses the recommended structure for testing coroutines and DataStore.
 * Each test runs in its own `runTest` scope and creates its own isolated DataStore instance,
 * which prevents coroutine conflicts and ensures tests do not interfere with each other.
 */
@ExperimentalCoroutinesApi
class TokenManagerTest {

    // JUnit rule to create a temporary folder for our test DataStore files,
    // ensuring no real device storage is used.
    @get:Rule
    val temporaryFolder: TemporaryFolder = TemporaryFolder()

    @Test
    fun `saveToken and getToken should save and retrieve the token correctly`() = runTest {
        // ARRANGE
        // Create a test-specific DataStore and TokenManager for this test only
        val testDataStore = PreferenceDataStoreFactory.create {
            temporaryFolder.newFile("test_datastore.preferences_pb")
        }
        val tokenManager = TokenManager(testDataStore)
        val testToken = "my-secret-test-token-123"

        // ACT
        tokenManager.saveToken(testToken)
        val retrievedToken = tokenManager.getToken()
        val tokenFromFlow = tokenManager.getTokenFlow().first()

        // ASSERT
        assertEquals(testToken, retrievedToken)
        assertEquals(testToken, tokenFromFlow)
    }

    @Test
    fun `saveUserRole and getUserRole should save and retrieve the role correctly`() = runTest {
        // ARRANGE
        val testDataStore = PreferenceDataStoreFactory.create {
            temporaryFolder.newFile("test_datastore.preferences_pb")
        }
        val tokenManager = TokenManager(testDataStore)
        val testRole = "manager"

        // ACT
        tokenManager.saveUserRole(testRole)
        val retrievedRole = tokenManager.getUserRole()

        // ASSERT
        assertEquals(testRole, retrievedRole)
    }

    @Test
    fun `clearSession should remove token and role`() = runTest {
        // ARRANGE
        val testDataStore = PreferenceDataStoreFactory.create {
            temporaryFolder.newFile("test_datastore.preferences_pb")
        }
        val tokenManager = TokenManager(testDataStore)
        val testToken = "my-secret-test-token-123"
        val testRole = "manager"
        tokenManager.saveToken(testToken)
        tokenManager.saveUserRole(testRole)

        // Sanity check to make sure they were saved
        assertEquals(testToken, tokenManager.getToken())
        assertEquals(testRole, tokenManager.getUserRole())

        // ACT
        tokenManager.clearSession()

        // ASSERT
        assertNull("Token should be null after clearing session", tokenManager.getToken())
        assertNull("Role should be null after clearing session", tokenManager.getUserRole())
    }

    @Test
    fun `getToken returns null when no token has been saved`() = runTest {
        // ARRANGE
        val testDataStore = PreferenceDataStoreFactory.create {
            temporaryFolder.newFile("test_datastore.preferences_pb")
        }
        val tokenManager = TokenManager(testDataStore)

        // ACT
        val retrievedToken = tokenManager.getToken()

        // ASSERT
        assertNull(retrievedToken)
    }
}
