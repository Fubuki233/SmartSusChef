package com.smartsuschef.mobile.ui.dashboard

import android.os.Bundle
import android.view.Menu
import android.view.MenuItem
import androidx.appcompat.app.AppCompatActivity
import android.view.View
import com.smartsuschef.mobile.R
import com.smartsuschef.mobile.databinding.ActivityDashboardBinding
import dagger.hilt.android.AndroidEntryPoint
import androidx.navigation.fragment.NavHostFragment
import androidx.navigation.ui.setupWithNavController

@AndroidEntryPoint
class DashboardActivity : AppCompatActivity() {
    private lateinit var binding: ActivityDashboardBinding
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityDashboardBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setSupportActionBar(binding.toolbar)

        // Setup Navigation as before...
        val navHostFragment = supportFragmentManager.findFragmentById(R.id.nav_host_fragment) as NavHostFragment
        binding.bottomNav.setupWithNavController(navHostFragment.navController)

        // Mirror the store context from Header.tsx
        supportActionBar?.subtitle = "Orchard Central | SmartSus Chef"
    }

    override fun onCreateOptionsMenu(menu: Menu?): Boolean {
        menuInflater.inflate(R.menu.dashboard_menu, menu)
        return true
    }

    override fun onOptionsItemSelected(item: MenuItem): Boolean {
        return when (item.itemId) {
            R.id.action_logout -> {
                // Call your AuthRepository.logout() here
                true
            }
            R.id.action_settings -> {
                // Navigate to Settings
                true
            }
            else -> super.onOptionsItemSelected(item)
        }
    }
}