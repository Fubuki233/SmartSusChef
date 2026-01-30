package com.smartsuschef.mobile.ui.auth

import android.content.Intent
import android.os.Bundle
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import com.smartsuschef.mobile.databinding.ActivityLoginBinding
import com.smartsuschef.mobile.network.dto.LoginRequest
import com.smartsuschef.mobile.ui.dashboard.DashboardActivity
import com.smartsuschef.mobile.util.Resource
import com.smartsuschef.mobile.util.showToast
import com.smartsuschef.mobile.util.visible
import com.smartsuschef.mobile.util.gone
import dagger.hilt.android.AndroidEntryPoint

@AndroidEntryPoint
class LoginActivity : AppCompatActivity() {

    private lateinit var binding: ActivityLoginBinding
    private val viewModel: LoginViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityLoginBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setupClickListeners()
        observeViewModel()
    }

    private fun setupClickListeners() {
        // --- View Toggle Logic ---

        // Switch to Forgot Password View
        binding.tvForgotPassword.setOnClickListener {
            binding.loginForm.gone()
            binding.forgotPasswordForm.visible()
        }

        // Switch back to Login View
        binding.btnBackToLogin.setOnClickListener {
            binding.forgotPasswordForm.gone()
            binding.loginForm.visible()
        }

        // --- Action Logic ---

        binding.btnSignIn.setOnClickListener {
            val username = binding.etUsername.text.toString().trim()
            val password = binding.etPassword.text.toString().trim()

            if (username.isNotEmpty() && password.isNotEmpty()) {
                viewModel.login(LoginRequest(username, password))
            } else {
                showToast("Please enter your credentials")
            }
        }

        binding.btnSendReset.setOnClickListener {
            val email = binding.etResetEmail.text.toString().trim()
            if (email.isNotEmpty()) {
                // Simulate sending reset link
                showToast("Reset link sent to $email")
                // Toggle back to login automatically after success
                binding.forgotPasswordForm.gone()
                binding.loginForm.visible()
            } else {
                showToast("Please enter your email address")
            }
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
        startActivity(Intent(this, DashboardActivity::class.java))
        finish()
    }
}