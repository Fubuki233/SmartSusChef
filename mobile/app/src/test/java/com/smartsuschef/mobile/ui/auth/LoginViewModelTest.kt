package com.smartsuschef.mobile.ui.auth

import androidx.arch.core.executor.testing.InstantTaskExecutorRule
import com.smartsuschef.mobile.data.repository.AuthRepository
import com.smartsuschef.mobile.network.dto.LoginRequest
import com.smartsuschef.mobile.network.dto.LoginResponse
import com.smartsuschef.mobile.network.dto.UserDto
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.TestDispatcher
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.mockito.Mock
import org.mockito.junit.MockitoJUnitRunner
import org.mockito.kotlin.verify
import org.mockito.kotlin.whenever

/**
 * Unit tests for the LoginViewModel.
 */
@ExperimentalCoroutinesApi
@RunWith(MockitoJUnitRunner::class)
class LoginViewModelTest {

    // This rule swaps the background executor used by the Architecture Components with a
    // different one which executes each task synchronously. Essential for LiveData testing.
    @get:Rule
    val instantTaskExecutorRule = InstantTaskExecutorRule()

    // Mock the repository dependency, since we only want to test the ViewModel's logic
    @Mock
    private lateinit var mockAuthRepository: AuthRepository

    // The ViewModel we are going to test
    private lateinit var viewModel: LoginViewModel

    // Test dispatcher for coroutines
    private val testDispatcher: TestDispatcher = UnconfinedTestDispatcher()

    @Before
    fun setUp() {
        // Set the main coroutines dispatcher for unit testing.
        Dispatchers.setMain(testDispatcher)
        // Initialize the ViewModel with the mock repository before each test
        viewModel = LoginViewModel(mockAuthRepository)
    }

    @After
    fun tearDown() {
        // Reset the main dispatcher to the original one after the test is completed.
        Dispatchers.resetMain()
    }

    @Test
    fun `login with valid credentials should return success`() = runTest {
        // ARRANGE
        // 1. Define the input for our test
        val loginRequest = LoginRequest("test", "password")

        // 2. Define the expected successful output from our mock repository
        val mockUser = UserDto("id", "test", "Test User", "email", "employee", "Active", "2026-02-08T00:00:00", "2026-02-08T00:00:00")
        val mockResponse = LoginResponse("fake-token", mockUser, storeSetupRequired = false)
        val expectedResult = Resource.Success(mockResponse)

        // 3. Program the mock repository to return the expected result when `login` is called
        whenever(mockAuthRepository.login(loginRequest)).thenReturn(expectedResult)

        // ACT
        // 4. Call the function on the ViewModel that we want to test
        viewModel.login(loginRequest)

        // ASSERT
        // 5. Check that the LiveData holds the expected successful result
        val actualResult = viewModel.loginResponse.value
        assertTrue(actualResult is Resource.Success)
        assertEquals(expectedResult.data, (actualResult as Resource.Success).data)
    }

    @Test
    fun `login with invalid credentials should return error`() = runTest {
        // ARRANGE
        // 1. Define the input for our test
        val loginRequest = LoginRequest("wrong", "user")

        // 2. Define the expected error output from our mock repository
        val expectedResult = Resource.Error<LoginResponse>("Invalid credentials")

        // 3. Program the mock repository to return the expected error
        whenever(mockAuthRepository.login(loginRequest)).thenReturn(expectedResult)

        // ACT
        // 4. Call the function on the ViewModel
        viewModel.login(loginRequest)

        // ASSERT
        // 5. Check that the LiveData holds the expected error result
        val actualResult = viewModel.loginResponse.value
        assertTrue(actualResult is Resource.Error)
        assertEquals(expectedResult.message, (actualResult as Resource.Error).message)
    }

    @Test
    fun `isUserLoggedIn should return true when repository indicates user is logged in`() {
        // ARRANGE
        // Program the mock repository to return true for isUserLoggedIn()
        whenever(mockAuthRepository.isUserLoggedIn()).thenReturn(true)

        // ACT
        val result = viewModel.isUserLoggedIn()

        // ASSERT
        assertTrue(result)
        // Verify that the repository's method was called
        verify(mockAuthRepository).isUserLoggedIn()
    }

    @Test
    fun `isUserLoggedIn should return false when repository indicates user is not logged in`() {
        // ARRANGE
        // Program the mock repository to return false for isUserLoggedIn()
        whenever(mockAuthRepository.isUserLoggedIn()).thenReturn(false)

        // ACT
        val result = viewModel.isUserLoggedIn()

        // ASSERT
        assertTrue(!result)
        // Verify that the repository's method was called
        verify(mockAuthRepository).isUserLoggedIn()
    }
}
