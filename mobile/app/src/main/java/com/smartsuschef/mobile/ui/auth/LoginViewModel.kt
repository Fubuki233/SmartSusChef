package com.smartsuschef.mobile.ui.auth
import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.smartsuschef.mobile.data.repository.AuthRepository
import com.smartsuschef.mobile.network.dto.LoginRequest
import com.smartsuschef.mobile.network.dto.LoginResponse
import com.smartsuschef.mobile.util.Resource
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class LoginViewModel @Inject constructor(
    private val repository: AuthRepository
) : ViewModel() {

    private val _loginResponse = MutableLiveData<Resource<LoginResponse>>()
    val loginResponse: LiveData<Resource<LoginResponse>> = _loginResponse

    /**
     * Called when user clicks "Sign In"
     */
    fun login(request: LoginRequest) {
        _loginResponse.value = Resource.Loading()
        viewModelScope.launch {
            val result = repository.login(request)
            _loginResponse.value = result
        }
    }

    /**
     * Checks if a session already exists to skip the login screen
     */
    fun isUserLoggedIn() = repository.isUserLoggedIn()
}