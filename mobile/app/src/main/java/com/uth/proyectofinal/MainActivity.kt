package com.uth.proyectofinal

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.platform.LocalContext
import androidx.core.content.ContextCompat
import com.uth.proyectofinal.push.PushEvents
import com.uth.proyectofinal.push.PushNotifications
import com.uth.proyectofinal.ui.navigation.AppNavHost
import com.uth.proyectofinal.ui.theme.PetCareTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        revisarApertura(intent)
        setContent {
            PetCareTheme {
                NotificationPermission()
                AppNavHost((application as PetCareApp).container)
            }
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        revisarApertura(intent)
    }

    private fun revisarApertura(intent: Intent?) {
        val desdeApp = intent?.getStringExtra(PushNotifications.EXTRA_ABRIR) == PushNotifications.ABRIR_NOTIFICACIONES
        val desdeFirebase = intent?.hasExtra(PushNotifications.EXTRA_TIPO) == true
        if (desdeApp || desdeFirebase) {
            intent?.removeExtra(PushNotifications.EXTRA_ABRIR)
            intent?.removeExtra(PushNotifications.EXTRA_TIPO)
            PushEvents.solicitarAbrir()
        }
    }
}

@Composable
private fun NotificationPermission() {
    val context = LocalContext.current
    val launcher = rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) { }
    LaunchedEffect(Unit) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU &&
            ContextCompat.checkSelfPermission(context, Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED
        ) {
            launcher.launch(Manifest.permission.POST_NOTIFICATIONS)
        }
    }
}
