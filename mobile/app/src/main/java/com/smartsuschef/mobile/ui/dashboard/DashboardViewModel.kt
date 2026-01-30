package com.smartsuschef.mobile.ui.dashboard

import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.smartsuschef.mobile.data.TokenManager
import com.smartsuschef.mobile.data.repository.AuthRepository
import com.smartsuschef.mobile.data.repository.UsersRepository
import com.smartsuschef.mobile.util.Resource
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class DashboardViewModel @Inject constructor(
    private val authRepository: AuthRepository,
    private val usersRepository: UsersRepository,
    private val tokenManager: TokenManager
) : ViewModel() {

    private val _username = MutableLiveData<String>()
    val username: LiveData<String> = _username

    private val _userRole = MutableLiveData<String>()
    val userRole: LiveData<String> = _userRole

    init {
        loadUserInfo()
    }

    private fun loadUserInfo() {
        viewModelScope.launch {
            _userRole.value = tokenManager.getUserRole() ?: "Employee"

            when (val userResult = usersRepository.getCurrentUser()) {
                is Resource.Success -> {
                    _username.value = userResult.data?.name ?: "User"
                }
                is Resource.Error -> {
                    _username.value = "User" // Fallback on error
                }
                is Resource.Loading -> {
                    // Optionally handle loading state
                }
            }
        }
    }

    fun logout() {
        viewModelScope.launch {
            authRepository.logout()
        }
    }
}