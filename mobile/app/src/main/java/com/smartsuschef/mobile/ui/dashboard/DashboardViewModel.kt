package com.smartsuschef.mobile.ui.dashboard

import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.smartsuschef.mobile.data.TokenManager
import com.smartsuschef.mobile.data.repository.AuthRepository
import com.smartsuschef.mobile.data.repository.UsersRepository
import com.smartsuschef.mobile.data.repository.StoreRepository
import com.smartsuschef.mobile.util.Resource
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class DashboardViewModel @Inject constructor(
    private val authRepository: AuthRepository,
    private val usersRepository: UsersRepository,
    private val storeRepository: StoreRepository,
    private val tokenManager: TokenManager
) : ViewModel() {

    private val _username = MutableLiveData<String>()
    val username: LiveData<String> = _username

    private val _userRole = MutableLiveData<String>()
    val userRole: LiveData<String> = _userRole

    private val _storeName = MutableLiveData<String>()
    val storeName: LiveData<String> = _storeName

    private val _outletLocation = MutableLiveData<String>()
    val outletLocation: LiveData<String> = _outletLocation

    private val _isLoading = MutableLiveData<Boolean>()
    val isLoading: LiveData<Boolean> = _isLoading

    init {
        loadUserInfo()
    }

    private fun loadUserInfo() {
        viewModelScope.launch {
            _isLoading.value = true // Start loading indicator
            _userRole.value = tokenManager.getUserRole() ?: "Employee"

            when (val userResult = usersRepository.getCurrentUser()) {
                is Resource.Success -> {
                    _username.value = userResult.data?.name ?: "User"
                    fetchStoreInfo()
                }
                is Resource.Error -> {
                    _username.value = "User"
                    _isLoading.value = false
                }
                is Resource.Loading -> {
                    _isLoading.value = true
                }
            }
        }
    }
    private suspend fun fetchStoreInfo() {
        when (val storeResult = storeRepository.getStore()) {
            is Resource.Success -> {
                // API call succeeds, Elvis operator to handle empty fields in JSON
                _storeName.value = storeResult.data?.storeName ?: "Store"
                _outletLocation.value = storeResult.data?.outletLocation ?: "Unknown"
                _isLoading.value = false
            }
            is Resource.Error -> {
                // When API call has failed, to use fallback values
                _storeName.value = "SmartSus Chef"
                _outletLocation.value = "Offline Mode"
                _isLoading.value = false
            }
            else -> {
                _isLoading.value = true
            }
        }
    }

    fun logout() {
        viewModelScope.launch {
            authRepository.logout()
        }
    }
}