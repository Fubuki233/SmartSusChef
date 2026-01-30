package com.smartsuschef.mobile.ui.auth

import android.os.Bundle
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import com.smartsuschef.mobile.network.dto.LoginRequest
import com.smartsuschef.mobile.util.Resource
import com.smartsuschef.mobile.util.gone
import com.smartsuschef.mobile.util.showToast
import com.smartsuschef.mobile.util.visible
import dagger.hilt.android.AndroidEntryPoint

@AndroidEntryPoint
class LoginActivity : AppCompatActivity() {
    private lateinit var binding: ActivityLoginBinding
    private val viewModel: LoginViewModel by viewModels()
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // 1. Check session first
        if (viewModel.isUserLoggedIn()) {
            navigateToDashboard()
            return
        }

        // 2. Setup ViewBinding for XML
        binding = ActivityLoginBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setupClickListeners()
        observeViewModel()
    }

    private fun setupClickListeners() {
        binding.btnSignIn.setOnClickListener {
            val username = binding.etUsername.text.toString()
            val password = binding.etPassword.text.toString()

            if (username.isNotEmpty() && password.isNotEmpty()) {
                viewModel.login(LoginRequest(username, password))
            } else {
                showToast("Please enter credentials")
            }
        }

        binding.tvForgotPassword.setOnClickListener {
            // Logic for image_8c0719.png goes here later
        }
    }

    private fun observeViewModel() {
        viewModel.loginResponse.observe(this) { resource ->
            when (resource) {
                is Resource.Loading -> {
                    binding.progressBar.visible()
                    binding.btnSignIn.isEnabled = false
                }
                is Resource.Success -> {
                    binding.progressBar.gone()
                    showToast("Welcome back!")
                    navigateToDashboard()
                }
                is Resource.Error -> {
                    binding.progressBar.gone()
                    binding.btnSignIn.isEnabled = true
                    showToast(resource.message ?: "Login Failed")
                }
            }
        }
    }

    private fun navigateToDashboard() {
        val intent = Intent(this, DashboardActivity::class.java)
        startActivity(intent)
        finish() // Closes LoginActivity so user can't "back" into it
    }
}