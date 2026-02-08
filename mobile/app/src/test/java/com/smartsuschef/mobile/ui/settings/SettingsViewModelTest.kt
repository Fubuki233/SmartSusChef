package com.smartsuschef.mobile.ui.settings

import androidx.arch.core.executor.testing.InstantTaskExecutorRule
import com.smartsuschef.mobile.data.repository.AuthRepository
import com.smartsuschef.mobile.data.repository.UsersRepository
import com.smartsuschef.mobile.network.dto.ChangePasswordRequest
import com.smartsuschef.mobile.network.dto.UpdateProfileRequest
import com.smartsuschef.mobile.network.dto.UserDto
import com.smartsuschef.mobile.util.Resource
import com.smartsuschef.mobile.util.EmailValidator
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.TestDispatcher
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.mockito.Mock
import org.mockito.junit.MockitoJUnitRunner
import org.mockito.kotlin.any
import org.mockito.kotlin.eq
import org.mockito.kotlin.never
import org.mockito.kotlin.verify
import org.mockito.kotlin.whenever
import org.mockito.MockitoAnnotations

@ExperimentalCoroutinesApi
@RunWith(MockitoJUnitRunner::class)
class SettingsViewModelTest {

    // Rule for LiveData testing
    @get:Rule
    val instantTaskExecutorRule = InstantTaskExecutorRule()

    // Mocks for all four repository dependencies
    @Mock
    private lateinit var mockAuthRepository: AuthRepository
    @Mock
    private lateinit var mockUsersRepository: UsersRepository
    @Mock
    private lateinit var mockEmailValidator: EmailValidator

    private lateinit var viewModel: SettingsViewModel
    private val testDispatcher: TestDispatcher = UnconfinedTestDispatcher()

    private val dummyUser = UserDto(
        id = "user-123",
        username = "testuser",
        name = "Test User",
        email = "test@example.com",
        role = "Employee",
        status = "Active",
        createdAt = "2026-02-08T00:00:00",
        updatedAt = "2026-02-08T00:00:00"
    )

    @Before
    fun setUp() {
        MockitoAnnotations.openMocks(this)

        Dispatchers.setMain(testDispatcher)

        // Mock email validation to return true for valid emails
        whenever(mockEmailValidator.isValid(any())).thenAnswer { invocation ->
            val email = invocation.getArgument<String>(0)
            email.contains("@") && email.contains(".")  // Simple test validation
        }

        runTest {
            whenever(mockUsersRepository.getCurrentUser()).thenReturn(Resource.Success(dummyUser))
        }

        // Pass the mock validator
        viewModel = SettingsViewModel(mockAuthRepository, mockUsersRepository, mockEmailValidator)
    }

    @After
    fun tearDown() {
        Dispatchers.resetMain()
    }

    // --- Initial Load Tests ---
    @Test
    fun `loadCurrentUser when succeeds should update currentUser and stop loading`() = runTest {
        // ARRANGE - done in @Before, ensures currentUser is dummyUser

        // ASSERT
        assertEquals(dummyUser, viewModel.currentUser.value)
        assertEquals(false, viewModel.isLoadingProfile.value)
        assertNull(viewModel.profileUpdateResult.value)
        verify(mockUsersRepository).getCurrentUser() // Verify it was called once in init
    }

    @Test
    fun `loadCurrentUser when fails should update profileResultWithError and stop loading`() = runTest {
        // ARRANGE
        val errorMessage = "Failed to fetch user"
        whenever(mockUsersRepository.getCurrentUser()).thenReturn(Resource.Error(errorMessage))

        // ACT - Re-init ViewModel to pick up new mock stub
        viewModel = SettingsViewModel(mockAuthRepository, mockUsersRepository, mockEmailValidator)

        // ASSERT
        assertNull(viewModel.currentUser.value)
        assertEquals(false, viewModel.isLoadingProfile.value)
        assertEquals("Failed to load user info: $errorMessage", viewModel.profileUpdateResult.value)
    }

    // --- Update Profile Tests ---
    @Test
    fun `updateProfile when succeeds should update currentUser and profileResult`() = runTest {
        // ARRANGE
        val newName = "Updated Name"
        val newEmail = "updated@example.com"
        val updatedUserDto = dummyUser.copy(name = newName, email = newEmail)
        whenever(mockUsersRepository.updateUser(any())).thenReturn(Resource.Success(updatedUserDto))

        // ACT
        viewModel.updateProfile(newName, newEmail)

        // ASSERT
        assertEquals(updatedUserDto, viewModel.currentUser.value)
        assertEquals("Profile updated successfully", viewModel.profileUpdateResult.value)
        assertEquals(false, viewModel.isLoadingProfile.value)
        verify(mockUsersRepository).updateUser(eq(UpdateProfileRequest(name = newName, email = newEmail)))
    }

    @Test
    fun `updateProfile when fails should update profileResultWithError`() = runTest {
        // ARRANGE
        val newName = "Updated Name"
        val newEmail = "updated@example.com"
        val errorMessage = "Failed to update profile"
        whenever(mockUsersRepository.updateUser(any())).thenReturn(Resource.Error(errorMessage))

        // ACT
        viewModel.updateProfile(newName, newEmail)

        // ASSERT
        assertEquals("Failed to update profile: $errorMessage", viewModel.profileUpdateResult.value)
        assertEquals(false, viewModel.isLoadingProfile.value)
    }

    @Test
    fun `updateProfile when name is blank should show validation error`() = runTest {
        // ACT
        viewModel.updateProfile("", "valid@email.com")

        // ASSERT
        assertEquals("Name cannot be empty", viewModel.profileUpdateResult.value)
        assertEquals(false, viewModel.isLoadingProfile.value) // Should not even go to repository
        verify(mockUsersRepository, never()).updateUser(any())
    }

    @Test
    fun `updateProfile when invalid email should show validation error`() = runTest {
        // ACT
        viewModel.updateProfile("Valid Name", "invalid-email")

        // ASSERT
        assertEquals("Please enter a valid email address", viewModel.profileUpdateResult.value)
        assertEquals(false, viewModel.isLoadingProfile.value)
        verify(mockUsersRepository, never()).updateUser(any())
    }

    // --- Change Password Tests ---
    @Test
    fun `changePassword when succeeds should update passwordResult`() = runTest {
        // ARRANGE
        val request = ChangePasswordRequest("oldPass", "newPass")
        whenever(mockAuthRepository.changePassword(request)).thenReturn(Resource.Success(Unit))

        // ACT
        viewModel.changePassword("oldPass", "newPass", "newPass")

        // ASSERT
        assertEquals("Password changed successfully", viewModel.passwordUpdateResult.value)
        assertEquals(false, viewModel.isLoadingPassword.value)
        verify(mockAuthRepository).changePassword(request)
    }

    @Test
    fun `changePassword when fails should update passwordResultWithError`() = runTest {
        // ARRANGE
        val request = ChangePasswordRequest("oldPass", "newPass")
        val errorMessage = "Current password incorrect"
        whenever(mockAuthRepository.changePassword(request)).thenReturn(Resource.Error(errorMessage))

        // ACT
        viewModel.changePassword("oldPass", "newPass", "newPass")

        // ASSERT
        assertEquals("Failed to change password: $errorMessage", viewModel.passwordUpdateResult.value)
        assertEquals(false, viewModel.isLoadingPassword.value)
    }

    @Test
    fun `changePassword when new password is blank should show validation error`() = runTest {
        // ACT
        viewModel.changePassword("oldPass", "", "")

        // ASSERT
        assertEquals("New password is required", viewModel.passwordUpdateResult.value)
        assertEquals(false, viewModel.isLoadingPassword.value)
        verify(mockAuthRepository, never()).changePassword(any())
    }

    @Test
    fun `changePassword when passwords do not match should show validation error`() = runTest {
        // ACT
        viewModel.changePassword("oldPass", "newPass", "mismatch")

        // ASSERT
        assertEquals("Passwords do not match", viewModel.passwordUpdateResult.value)
        assertEquals(false, viewModel.isLoadingPassword.value)
        verify(mockAuthRepository, never()).changePassword(any())
    }

    @Test
    fun `changePassword when new password is same as current should show validation error`() = runTest {
        // ACT
        viewModel.changePassword("oldPass", "oldPass", "oldPass")

        // ASSERT
        assertEquals("New password must be different from current password", viewModel.passwordUpdateResult.value)
        assertEquals(false, viewModel.isLoadingPassword.value)
        verify(mockAuthRepository, never()).changePassword(any())
    }
}
