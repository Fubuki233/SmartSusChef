package com.smartsuschef.mobile.ui.dashboard

import androidx.arch.core.executor.testing.InstantTaskExecutorRule
import com.smartsuschef.mobile.data.TokenManager
import com.smartsuschef.mobile.data.repository.AuthRepository
import com.smartsuschef.mobile.data.repository.StoreRepository
import com.smartsuschef.mobile.data.repository.UsersRepository
import com.smartsuschef.mobile.network.dto.StoreDto
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
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.mockito.Mock
import org.mockito.junit.MockitoJUnitRunner
import org.mockito.kotlin.verify
import org.mockito.kotlin.whenever
import org.mockito.kotlin.never

/**
 * Unit tests for the DashboardViewModel.
 */
@ExperimentalCoroutinesApi
@RunWith(MockitoJUnitRunner::class)
class DashboardViewModelTest {

    @get:Rule
    val instantTaskExecutorRule = InstantTaskExecutorRule()

    @Mock
    private lateinit var mockAuthRepository: AuthRepository
    @Mock
    private lateinit var mockUsersRepository: UsersRepository
    @Mock
    private lateinit var mockStoreRepository: StoreRepository
    @Mock
    private lateinit var mockTokenManager: TokenManager

    private val testDispatcher: TestDispatcher = UnconfinedTestDispatcher()

    @Before
    fun setUp() {
        Dispatchers.setMain(testDispatcher)
    }

    @After
    fun tearDown() {
        Dispatchers.resetMain()
    }

    @Test
    fun `init when all calls succeed should update all LiveData correctly`() = runTest {
        // ARRANGE
        // 1. Prepare mock data and successful responses from all repositories
        val mockUserName = "Test User"
        val mockUserRole = "Manager"
        val mockStoreName = "Main Street Cafe"
        val mockOutletLocation = "Orchard Road"

        val mockUserDto = UserDto("id", "username", mockUserName, "email", mockUserRole, "Active")
        val mockStoreDto = StoreDto(1, "company", "uen", mockStoreName, mockOutletLocation, "123", "date", 0.0, 0.0, "SG", "address", true, "date", "date")

        // 2. Program the mocks to return the successful data
        whenever(mockTokenManager.getUserRole()).thenReturn(mockUserRole)
        whenever(mockUsersRepository.getCurrentUser()).thenReturn(Resource.Success(mockUserDto))
        whenever(mockStoreRepository.getStore()).thenReturn(Resource.Success(mockStoreDto))

        // ACT
        // 3. Initialize the ViewModel, which triggers the `init` block and `loadUserInfo`
        val viewModel = DashboardViewModel(
            mockAuthRepository,
            mockUsersRepository,
            mockStoreRepository,
            mockTokenManager
        )

        // ASSERT
        // 4. Verify that all LiveData objects have been updated with the correct values
        assertEquals(mockUserName, viewModel.username.value)
        assertEquals(mockUserRole, viewModel.userRole.value)
        assertEquals(mockStoreName, viewModel.storeName.value)
        assertEquals(mockOutletLocation, viewModel.outletLocation.value)
        assertEquals(false, viewModel.isLoading.value) // Should be false after loading finishes
    }

    @Test
    fun `logout should call authRepository logout`() = runTest {
        // ARRANGE
        // We must provide default, non-null responses for the calls made in the ViewModel's init block.
        val dummyUser = UserDto("id", "user", "name", "email", "role", "Active")
        whenever(mockTokenManager.getUserRole()).thenReturn("Employee")
        whenever(mockUsersRepository.getCurrentUser()).thenReturn(Resource.Success(dummyUser))
        // The store call won't be made if the user call returns a valid user, but it's good practice to stub it.
        val dummyStore = StoreDto(1, "", "", "", "", "", "", 0.0, 0.0, null, null, true, "", "")
        whenever(mockStoreRepository.getStore()).thenReturn(Resource.Success(dummyStore))


        val viewModel = DashboardViewModel(
            mockAuthRepository,
            mockUsersRepository,
            mockStoreRepository,
            mockTokenManager
        )

        // ACT
        viewModel.logout()

        // ASSERT
        // Verify that the repository's logout method was called exactly once
        verify(mockAuthRepository).logout()
    }

    @Test
    fun `init when getUser fails should set default username and stop loading`() = runTest {
        // ARRANGE
        // 1. Program the usersRepository to return an error
        val errorMessage = "Failed to load user"
        whenever(mockUsersRepository.getCurrentUser()).thenReturn(Resource.Error(errorMessage))
        whenever(mockTokenManager.getUserRole()).thenReturn("Employee") // Still need to mock this

        // ACT
        // 2. Initialize the ViewModel
        val viewModel = DashboardViewModel(
            mockAuthRepository,
            mockUsersRepository,
            mockStoreRepository,
            mockTokenManager
        )

        // ASSERT
        // 3. Verify that the username is set to a default value
        assertEquals("User", viewModel.username.value)

        // 4. Verify that the loading state is set to false
        assertEquals(false, viewModel.isLoading.value)

        // 5. CRUCIAL: Verify that the second repository call was never made
        verify(mockStoreRepository, never()).getStore()
    }

    @Test
    fun `init when getStore fails should set default store info and stop loading`() = runTest {
        // ARRANGE
        // 1. Prepare mock data for a successful user call and a failed store call
        val mockUserName = "Test User"
        val mockUserRole = "Manager"
        val errorMessage = "Failed to load store"

        val mockUserDto = UserDto("id", "username", mockUserName, "email", mockUserRole, "Active")

        // 2. Program the mocks
        whenever(mockTokenManager.getUserRole()).thenReturn(mockUserRole)
        whenever(mockUsersRepository.getCurrentUser()).thenReturn(Resource.Success(mockUserDto))
        whenever(mockStoreRepository.getStore()).thenReturn(Resource.Error(errorMessage))

        // ACT
        // 3. Initialize the ViewModel
        val viewModel = DashboardViewModel(
            mockAuthRepository,
            mockUsersRepository,
            mockStoreRepository,
            mockTokenManager
        )

        // ASSERT
        // 4. Verify that the username is still set correctly
        assertEquals(mockUserName, viewModel.username.value)
        assertEquals(mockUserRole, viewModel.userRole.value)

        // 5. Verify that store info uses fallback values
        assertEquals("SmartSus Chef", viewModel.storeName.value)
        assertEquals("Offline Mode", viewModel.outletLocation.value)

        // 6. Verify that the loading state is set to false
        assertEquals(false, viewModel.isLoading.value)
    }
}
