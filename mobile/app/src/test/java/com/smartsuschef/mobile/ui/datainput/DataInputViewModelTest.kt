package com.smartsuschef.mobile.ui.datainput

import androidx.arch.core.executor.testing.InstantTaskExecutorRule
import com.smartsuschef.mobile.data.repository.IngredientsRepository
import com.smartsuschef.mobile.data.repository.RecipesRepository
import com.smartsuschef.mobile.data.repository.SalesRepository
import com.smartsuschef.mobile.data.repository.WastageRepository
import com.smartsuschef.mobile.network.dto.CreateSalesDataRequest
import com.smartsuschef.mobile.network.dto.CreateWastageDataRequest
import com.smartsuschef.mobile.network.dto.SalesDataDto
import com.smartsuschef.mobile.network.dto.UpdateSalesDataRequest
import com.smartsuschef.mobile.network.dto.UpdateWastageDataRequest
import com.smartsuschef.mobile.network.dto.WastageDataDto
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
import org.mockito.kotlin.any
import org.mockito.kotlin.argumentCaptor
import org.mockito.kotlin.eq
import org.mockito.kotlin.verify
import org.mockito.kotlin.whenever
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

/**
 * Unit tests for the DataInputViewModel.
 */
@ExperimentalCoroutinesApi
@RunWith(MockitoJUnitRunner::class)
class DataInputViewModelTest {

    // Rule for LiveData testing
    @get:Rule
    val instantTaskExecutorRule = InstantTaskExecutorRule()

    // Mocks for all four repository dependencies
    @Mock
    private lateinit var mockSalesRepository: SalesRepository
    @Mock
    private lateinit var mockWastageRepository: WastageRepository
    @Mock
    private lateinit var mockIngredientsRepository: IngredientsRepository
    @Mock
    private lateinit var mockRecipesRepository: RecipesRepository

    private lateinit var viewModel: DataInputViewModel
    private val testDispatcher: TestDispatcher = UnconfinedTestDispatcher()

    @Before
    fun setUp() {
        Dispatchers.setMain(testDispatcher)
        // Stub the repository calls that happen in the ViewModel's init block
        runTest {
            whenever(mockIngredientsRepository.getAll()).thenReturn(Resource.Success(emptyList()))
            whenever(mockRecipesRepository.getAll()).thenReturn(Resource.Success(emptyList()))
        }
        // Initialize the ViewModel with all the mocks
        viewModel = DataInputViewModel(
            mockSalesRepository,
            mockWastageRepository,
            mockIngredientsRepository,
            mockRecipesRepository
        )
    }

    @After
    fun tearDown() {
        Dispatchers.resetMain()
    }

    @Test
    fun `submitData in sales mode with new entry should call salesRepository create and return success`() = runTest {
        // ARRANGE
        // 1. Set the ViewModel to sales mode and select an item
        viewModel.setMode(isSales = true)
        viewModel.onItemSelected(itemId = "recipe-123", itemName = "Pizza")

        // 2. Define the successful response from the repository
        val todayStr = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())
        val mockSuccessfulResponse = SalesDataDto(
            id = "new-sale-id-456",
            date = todayStr,
            recipeId = "recipe-123",
            recipeName = "Pizza",
            quantity = 2,
            createdAt = todayStr,
            updatedAt = todayStr
        )
        whenever(mockSalesRepository.create(any())).thenReturn(Resource.Success(mockSuccessfulResponse))

        // 3. Use a captor to verify the exact request sent to the repository
        val requestCaptor = argumentCaptor<CreateSalesDataRequest>()

        // ACT
        // 4. Call the function we want to test
        viewModel.submitData(quantity = 2.0)

        // ASSERT
        // 5. Verify that the `create` method on the sales repository was called exactly once
        verify(mockSalesRepository).create(requestCaptor.capture())

        // 6. Verify the content of the request object sent to the repository
        assertEquals("recipe-123", requestCaptor.firstValue.recipeId)
        assertEquals(2, requestCaptor.firstValue.quantity)

        // 7. Verify that the submitStatus LiveData reflects the success
        val status = viewModel.submitStatus.value
        assertTrue("Submit status should be Success", status is Resource.Success)
        assertEquals("Entry saved successfully!", (status as Resource.Success).message)

        // 8. Verify that the new entry was added to the recent entries list
        val recentEntries = viewModel.recentEntries.value
        assertTrue("Recent entries list should not be empty", recentEntries?.isNotEmpty() == true)
        assertEquals("new-sale-id-456", recentEntries?.first()?.id)
        assertEquals("Pizza", recentEntries?.first()?.name)
    }

    @Test
    fun `submitData in wastage mode with new entry should call wastageRepository create and return success`() = runTest {
        // ARRANGE
        // 1. Set the ViewModel to wastage mode for an ingredient
        viewModel.setMode(isSales = false)
        viewModel.setWastageType(WastageType.INGREDIENT)
        viewModel.onItemSelected(itemId = "ing-789", itemName = "Tomatoes")

        // 2. Define the successful response from the repository
        val todayStr = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())
        val mockSuccessfulResponse = WastageDataDto(
            id = "new-wastage-id-999",
            date = todayStr,
            ingredientId = "ing-789",
            recipeId = null,
            displayName = "Tomatoes",
            unit = "kg",
            quantity = 0.5,
            carbonFootprint = 1.0,
            createdAt = todayStr,
            updatedAt = todayStr
        )
        whenever(mockWastageRepository.create(any())).thenReturn(Resource.Success(mockSuccessfulResponse))

        // 3. Use a captor to verify the exact request sent to the repository
        val requestCaptor = argumentCaptor<CreateWastageDataRequest>()

        // ACT
        // 4. Call the function we want to test
        viewModel.submitData(quantity = 0.5)

        // ASSERT
        // 5. Verify that the `create` method on the wastage repository was called exactly once
        verify(mockWastageRepository).create(requestCaptor.capture())

        // 6. Verify the content of the request object
        assertEquals("ing-789", requestCaptor.firstValue.ingredientId)
        assertEquals(0.5, requestCaptor.firstValue.quantity, 0.0)

        // 7. Verify that the submitStatus LiveData reflects the success
        val status = viewModel.submitStatus.value
        assertTrue("Submit status should be Success", status is Resource.Success)
        assertEquals("Entry saved successfully!", (status as Resource.Success).message)

        // 8. Verify that the new entry was added to the recent entries list
        val recentEntries = viewModel.recentEntries.value
        assertTrue("Recent entries list should not be empty", recentEntries?.isNotEmpty() == true)
        assertEquals("new-wastage-id-999", recentEntries?.first()?.id)
        assertEquals("Tomatoes", recentEntries?.first()?.name)
        assertEquals(false, recentEntries?.first()?.isSales)
    }

    @Test
    fun `deleteEntry for sales should call salesRepository delete and update status`() = runTest {
        // ARRANGE
        // 1. Prepare mock responses for initial submissions and the delete operation
        val initialEntryId1 = "sales-initial-1"
        val initialEntryId2 = "sales-initial-2"
        val initialEntryName1 = "Pizza A"
        val initialEntryName2 = "Pizza B"
        val initialQuantity = 1.0
        val todayStr = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())
        val currentTimeStr = SimpleDateFormat("HH:mm", Locale.getDefault()).format(Date())

        val mockSalesDto1 = SalesDataDto(
            id = initialEntryId1,
            date = todayStr,
            recipeId = "rec-1",
            recipeName = initialEntryName1,
            quantity = initialQuantity.toInt(),
            createdAt = todayStr,
            updatedAt = todayStr
        )
        val mockSalesDto2 = SalesDataDto(
            id = initialEntryId2,
            date = todayStr,
            recipeId = "rec-2",
            recipeName = initialEntryName2,
            quantity = initialQuantity.toInt(),
            createdAt = todayStr,
            updatedAt = todayStr
        )

        // Mock repository calls for initial submissions
        whenever(mockSalesRepository.create(any()))
            .thenReturn(Resource.Success(mockSalesDto1)) // First create call
            .thenReturn(Resource.Success(mockSalesDto2)) // Second create call

        // 2. Perform initial submissions via ViewModel's public API to populate state
        viewModel.setMode(true) // Ensure sales mode
        viewModel.onItemSelected("rec-1", initialEntryName1)
        viewModel.submitData(initialQuantity) // Submits first entry

        viewModel.onItemSelected("rec-2", initialEntryName2)
        viewModel.submitData(initialQuantity) // Submits second entry

        // Ensure the ViewModel's recentEntries LiveData is updated after submissions
        assertEquals(2, viewModel.recentEntries.value?.size)

        // 3. Define the entry we want to delete (first submitted entry)
        val entryToDelete = RecentEntry(
            id = initialEntryId1,
            name = initialEntryName1,
            quantity = initialQuantity,
            unit = "plates", // Default unit for sales
            date = todayStr, // Match mock date
            time = currentTimeStr, // Match ViewModel logic format
            isSales = true
        )

        // 4. Program the mock sales repository to return success when delete is called
        whenever(mockSalesRepository.delete(entryToDelete.id)).thenReturn(Resource.Success(Unit))

        // ACT
        // 5. Call the function we want to test
        viewModel.deleteEntry(entryToDelete)

        // ASSERT
        // 6. Verify that salesRepository.delete was called with the correct ID
        verify(mockSalesRepository).delete(entryToDelete.id)

        // 7. Verify that the submitStatus LiveData reflects the success
        val status = viewModel.submitStatus.value
        assertTrue("Submit status should be Success", status is Resource.Success)
        assertEquals("Entry deleted successfully!", (status as Resource.Success).message)

        // 8. Verify that the entry is removed from the recent entries list
        val recentEntries = viewModel.recentEntries.value
        assertEquals("Recent entries should contain one entry", 1, recentEntries?.size)
        assertTrue("Recent entries should not contain the deleted entry", recentEntries?.contains(entryToDelete) == false)
        assertEquals(initialEntryId2, recentEntries?.first()?.id) // The other entry should remain
    }

    @Test
    fun `submitData in sales mode with existing entry should call salesRepository update and return success`() = runTest {
        // ARRANGE
        // 1. Prepare an initial entry that already exists in the ViewModel's state
        val existingEntryId = "sales-existing-123"
        val existingRecipeId = "rec-existing"
        val existingRecipeName = "Old Recipe"
        val initialQuantity = 5
        val updatedQuantity = 10
        val todayStr = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())

        // Mock the initial sales submission to populate the ViewModel's internal list
        val mockSalesDtoForInitial = SalesDataDto(
            id = existingEntryId,
            date = todayStr,
            recipeId = existingRecipeId,
            recipeName = existingRecipeName,
            quantity = initialQuantity,
            createdAt = todayStr,
            updatedAt = todayStr
        )
        whenever(mockSalesRepository.create(any())).thenReturn(Resource.Success(mockSalesDtoForInitial))

        viewModel.setMode(true)
        viewModel.onItemSelected(existingRecipeId, existingRecipeName)
        viewModel.submitData(initialQuantity.toDouble())

        // Ensure the entry is in the list
        assertEquals(1, viewModel.recentEntries.value?.size)
        assertEquals(initialQuantity.toDouble(), viewModel.recentEntries.value!!.first().quantity, 0.0)

        // 2. Prepare the mock response for the update operation
        val mockSalesDtoForUpdate = SalesDataDto(
            id = existingEntryId,
            date = todayStr,
            recipeId = existingRecipeId,
            recipeName = existingRecipeName,
            quantity = updatedQuantity,
            createdAt = todayStr,
            updatedAt = todayStr
        )
        whenever(mockSalesRepository.update(eq(existingEntryId), any())).thenReturn(Resource.Success(mockSalesDtoForUpdate))

        // 3. Use a captor to verify the update request sent to the repository
        val updateRequestCaptor = argumentCaptor<UpdateSalesDataRequest>()

        // ACT
        // 4. Call submitData with the existingEntryId to trigger an update
        viewModel.submitData(quantity = updatedQuantity.toDouble(), existingEntryId = existingEntryId)

        // ASSERT
        // 5. Verify that the `update` method on the sales repository was called
        verify(mockSalesRepository).update(eq(existingEntryId), updateRequestCaptor.capture())

        // 6. Verify the content of the update request
        assertEquals(updatedQuantity, updateRequestCaptor.firstValue.quantity)

        // 7. Verify that the submitStatus LiveData reflects the success
        val status = viewModel.submitStatus.value
        assertTrue("Submit status should be Success", status is Resource.Success)
        assertEquals("Entry saved successfully!", (status as Resource.Success).message)

        // 8. Verify that the entry in recentEntries is updated
        val recentEntries = viewModel.recentEntries.value
        assertEquals("Recent entries should contain one entry", 1, recentEntries?.size)
        assertEquals(existingEntryId, recentEntries!!.first().id)
        assertEquals(updatedQuantity.toDouble(), recentEntries.first().quantity, 0.0)
    }

    @Test
    fun `submitData in wastage mode with existing entry should call wastageRepository update and return success`() = runTest {
        // ARRANGE
        // 1. Prepare an initial entry that already exists in the ViewModel's state
        val existingEntryId = "wastage-existing-456"
        val existingIngredientId = "ing-existing"
        val existingIngredientName = "Old Tomatoes"
        val initialQuantity = 0.5
        val updatedQuantity = 1.0
        val todayStr = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())

        // Mock the initial wastage submission to populate the ViewModel's internal list
        val mockWastageDtoForInitial = WastageDataDto(
            id = existingEntryId,
            date = todayStr,
            ingredientId = existingIngredientId,
            recipeId = null,
            displayName = existingIngredientName,
            unit = "kg",
            quantity = initialQuantity,
            carbonFootprint = initialQuantity * 1.5,
            createdAt = todayStr,
            updatedAt = todayStr
        )
        whenever(mockWastageRepository.create(any())).thenReturn(Resource.Success(mockWastageDtoForInitial))

        viewModel.setMode(false) // Wastage mode
        viewModel.setWastageType(WastageType.INGREDIENT)
        viewModel.onItemSelected(existingIngredientId, existingIngredientName)
        viewModel.submitData(initialQuantity)

        // Ensure the entry is in the list
        assertEquals(1, viewModel.recentEntries.value?.size)
        assertEquals(initialQuantity, viewModel.recentEntries.value!!.first().quantity, 0.0)

        // 2. Prepare the mock response for the update operation
        val mockWastageDtoForUpdate = WastageDataDto(
            id = existingEntryId,
            date = todayStr,
            ingredientId = existingIngredientId,
            recipeId = null,
            displayName = existingIngredientName,
            unit = "kg",
            quantity = updatedQuantity,
            carbonFootprint = updatedQuantity * 1.5,
            createdAt = todayStr,
            updatedAt = todayStr
        )
        whenever(mockWastageRepository.update(eq(existingEntryId), any())).thenReturn(Resource.Success(mockWastageDtoForUpdate))

        // 3. Use a captor to verify the update request sent to the repository
        val updateRequestCaptor = argumentCaptor<UpdateWastageDataRequest>()

        // ACT
        // 4. Call submitData with the existingEntryId to trigger an update
        viewModel.submitData(quantity = updatedQuantity, existingEntryId = existingEntryId)

        // ASSERT
        // 5. Verify that the `update` method on the wastage repository was called
        verify(mockWastageRepository).update(eq(existingEntryId), updateRequestCaptor.capture())

        // 6. Verify the content of the update request
        assertEquals(updatedQuantity, updateRequestCaptor.firstValue.quantity, 0.0)
        assertEquals(existingIngredientId, updateRequestCaptor.firstValue.ingredientId)

        // 7. Verify that the submitStatus LiveData reflects the success
        val status = viewModel.submitStatus.value
        assertTrue("Submit status should be Success", status is Resource.Success)
        assertEquals("Entry saved successfully!", (status as Resource.Success).message)

        // 8. Verify that the entry in recentEntries is updated
        val recentEntries = viewModel.recentEntries.value
        assertEquals("Recent entries should contain one entry", 1, recentEntries?.size)
        assertEquals(existingEntryId, recentEntries!!.first().id)
        assertEquals(updatedQuantity, recentEntries.first().quantity, 0.0)
    }

    @Test
    fun `deleteEntry for wastage should call wastageRepository delete and update status`() = runTest {
        // ARRANGE
        // 1. Mock the initial submission that will create the entry we want to delete
        val entryIdToDelete = "wastage-delete-123"
        val entryNameToDelete = "Stale Bread"
        val todayStr = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())

        val mockWastageDtoForInitial = WastageDataDto(
            id = entryIdToDelete,
            date = todayStr,
            ingredientId = "ing-bread",
            recipeId = null,
            displayName = entryNameToDelete,
            unit = "loaf",
            quantity = 2.0,
            carbonFootprint = 3.0,
            createdAt = todayStr,
            updatedAt = todayStr
        )
        whenever(mockWastageRepository.create(any())).thenReturn(Resource.Success(mockWastageDtoForInitial))

        // 2. Perform the submission to populate the ViewModel's state
        viewModel.setMode(false) // Wastage mode
        viewModel.setWastageType(WastageType.INGREDIENT)
        viewModel.onItemSelected("ing-bread", entryNameToDelete)
        viewModel.submitData(2.0)

        // 3. Get the ACTUAL entry object from the ViewModel's LiveData. This is crucial.
        val entryToDelete = viewModel.recentEntries.value?.first()
        assertEquals(entryIdToDelete, entryToDelete?.id) // Sanity check

        // 4. Program the mock repository to return success when delete is called
        whenever(mockWastageRepository.delete(entryIdToDelete)).thenReturn(Resource.Success(Unit))

        // ACT
        // 5. Call the function we want to test with the actual object from the ViewModel
        viewModel.deleteEntry(entryToDelete!!)

        // ASSERT
        // 6. Verify that wastageRepository.delete was called with the correct ID
        verify(mockWastageRepository).delete(entryIdToDelete)

        // 7. Verify that the submitStatus LiveData reflects the success
        val status = viewModel.submitStatus.value
        assertTrue("Submit status should be Success", status is Resource.Success)
        assertEquals("Entry deleted successfully!", (status as Resource.Success).message)

        // 8. Verify that the entry is removed from the recent entries list
        val recentEntries = viewModel.recentEntries.value
                assertEquals("Recent entries list should now be empty", 0, recentEntries?.size)
            }
        
            @Test
            fun `submitData when repository returns error should update status to error`() = runTest {
                // ARRANGE
                // 1. Set the ViewModel to sales mode and select an item
                viewModel.setMode(isSales = true)
                viewModel.onItemSelected(itemId = "recipe-123", itemName = "Pizza")
        
                // 2. Program the mock repository to return an error
                val errorMessage = "Network failed"
                whenever(mockSalesRepository.create(any())).thenReturn(Resource.Error(errorMessage))
        
                // ACT
                // 3. Call the function we want to test
                viewModel.submitData(quantity = 2.0)
        
                // ASSERT
                // 4. Verify that the submitStatus LiveData reflects the error
                val status = viewModel.submitStatus.value
                assertTrue("Submit status should be Error", status is Resource.Error)
                assertEquals(errorMessage, (status as Resource.Error).message)
        
                // 5. Verify that the recent entries list was NOT updated
                val recentEntries = viewModel.recentEntries.value
                        assertTrue("Recent entries should be empty on failure", recentEntries.isNullOrEmpty())
                    }
                
                    @Test
                    fun `deleteEntry when repository returns error should update status to error`() = runTest {
                        // ARRANGE
                        // 1. Prepare an entry that exists in the ViewModel's internal list
                        val entryToDeleteId = "sales-delete-fail-123"
                        val entryToDeleteName = "Failed Delete Item"
                        val todayStr = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())
                
                        val mockSalesDtoForInitial = SalesDataDto(
                            id = entryToDeleteId,
                            date = todayStr,
                            recipeId = "rec-fail",
                            recipeName = entryToDeleteName,
                            quantity = 1,
                            createdAt = todayStr,
                            updatedAt = todayStr
                        )
                        whenever(mockSalesRepository.create(any())).thenReturn(Resource.Success(mockSalesDtoForInitial))
                
                        viewModel.setMode(true) // Sales mode
                        viewModel.onItemSelected("rec-fail", entryToDeleteName)
                        viewModel.submitData(1.0)
                
                        // Ensure the entry is in the list before deletion attempt
                        assertEquals(1, viewModel.recentEntries.value?.size)
                        val entryToAttemptDelete = viewModel.recentEntries.value?.first()!!
                
                        // 2. Program the mock repository to return an error for the delete operation
                        val errorMessage = "Failed to delete item"
                        whenever(mockSalesRepository.delete(entryToAttemptDelete.id)).thenReturn(Resource.Error(errorMessage))
                
                        // ACT
                        // 3. Call the function we want to test
                        viewModel.deleteEntry(entryToAttemptDelete)
                
                        // ASSERT
                        // 4. Verify that salesRepository.delete was called
                        verify(mockSalesRepository).delete(entryToAttemptDelete.id)
                
                        // 5. Verify that the submitStatus LiveData reflects the error
                        val status = viewModel.submitStatus.value
                        assertTrue("Submit status should be Error", status is Resource.Error)
                        assertEquals(errorMessage, (status as Resource.Error).message)
                
                        // 6. Verify that the entry was NOT removed from the recent entries list
                        val recentEntries = viewModel.recentEntries.value
                        assertEquals("Recent entries list should still contain the item", 1, recentEntries?.size)
                        assertTrue("The item should still be in the list", recentEntries?.contains(entryToAttemptDelete) == true)
                    }
                }
                