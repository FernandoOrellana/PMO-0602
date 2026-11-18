package com.uth.proyectofinal.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val Light = lightColorScheme(
    primary = Color(0xFF00695C),
    onPrimary = Color.White,
    primaryContainer = Color(0xFFA7F2E4),
    onPrimaryContainer = Color(0xFF00201B),
    secondary = Color(0xFF4A6360),
)

private val Dark = darkColorScheme(
    primary = Color(0xFF80CBC4),
    onPrimary = Color(0xFF003731),
    primaryContainer = Color(0xFF005048),
    onPrimaryContainer = Color(0xFFA7F2E4),
    secondary = Color(0xFFB1CCC8),
)

@Composable
fun PetCareTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = if (isSystemInDarkTheme()) Dark else Light,
        content = content,
    )
}
