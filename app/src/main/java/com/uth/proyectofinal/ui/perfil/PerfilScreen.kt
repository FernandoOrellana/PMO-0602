package com.uth.proyectofinal.ui.perfil

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.compose.viewModel
import com.uth.proyectofinal.data.local.SessionUser
import com.uth.proyectofinal.data.repository.AuthRepository
import com.uth.proyectofinal.ui.common.AppScaffold
import com.uth.proyectofinal.ui.common.ConfirmDialog
import com.uth.proyectofinal.ui.common.appFactory
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ExitToApp
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material3.Card
import androidx.compose.material3.Icon
import androidx.compose.ui.Alignment
import com.uth.proyectofinal.data.remote.dto.Rol
import com.uth.proyectofinal.ui.common.Avatar
import com.uth.proyectofinal.ui.common.EstadoBadge

class PerfilViewModel(private val auth: AuthRepository) : ViewModel() {

    val usuario: SessionUser? = auth.user

    private val _ocupado = MutableStateFlow(false)
    val ocupado: StateFlow<Boolean> = _ocupado.asStateFlow()

    private val _salio = MutableStateFlow(false)
    val salio: StateFlow<Boolean> = _salio.asStateFlow()

    fun cerrarSesion() {
        viewModelScope.launch {
            _ocupado.update { true }
            auth.logout()
            _salio.value = true
        }
    }
}

@Composable
fun PerfilScreen(onNotificaciones: () -> Unit, onLoggedOut: () -> Unit) {
    val vm: PerfilViewModel = viewModel(factory = appFactory { PerfilViewModel(it.auth) })
    val ocupado by vm.ocupado.collectAsStateWithLifecycle()
    val salio by vm.salio.collectAsStateWithLifecycle()
    var confirmar by remember { mutableStateOf(false) }

    LaunchedEffect(salio) {
        if (salio) onLoggedOut()
    }

    AppScaffold(title = "Perfil") { padding ->
        Column(
            modifier = Modifier.padding(padding).padding(24.dp).fillMaxSize(),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            val usuario = vm.usuario
            Card(Modifier.fillMaxWidth()) {
                Row(Modifier.padding(20.dp), verticalAlignment = Alignment.CenterVertically) {
                    Avatar(usuario?.nombre.orEmpty(), Modifier.size(56.dp))
                    Spacer(Modifier.width(16.dp))
                    Column(Modifier.weight(1f)) {
                        Text(usuario?.nombre.orEmpty(), style = MaterialTheme.typography.titleLarge)
                        Text(
                            usuario?.email.orEmpty(),
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                        Spacer(Modifier.height(8.dp))
                        EstadoBadge(rolLegible(usuario?.rol))
                    }
                }
            }
            Spacer(Modifier.height(4.dp))
            Button(
                onClick = onNotificaciones,
                shape = MaterialTheme.shapes.small,
                contentPadding = PaddingValues(vertical = 14.dp),
                modifier = Modifier.fillMaxWidth(),
            ) {
                Icon(Icons.Filled.Notifications, contentDescription = null, modifier = Modifier.size(18.dp))
                Spacer(Modifier.width(8.dp))
                Text("Notificaciones")
            }
            OutlinedButton(
                onClick = { confirmar = true },
                enabled = !ocupado,
                shape = MaterialTheme.shapes.small,
                contentPadding = PaddingValues(vertical = 14.dp),
                modifier = Modifier.fillMaxWidth(),
            ) {
                Icon(Icons.AutoMirrored.Filled.ExitToApp, contentDescription = null, modifier = Modifier.size(18.dp))
                Spacer(Modifier.width(8.dp))
                Text("Cerrar sesión")
            }
        }
    }

    if (confirmar) {
        ConfirmDialog(
            title = "Cerrar sesión",
            text = "¿Seguro que deseas salir de la aplicación?",
            confirmText = "Salir",
            onConfirm = {
                confirmar = false
                vm.cerrarSesion()
            },
            onDismiss = { confirmar = false },
        )
    }
}

private fun rolLegible(rol: String?): String = when (rol) {
    Rol.DUENO -> "Dueño"
    Rol.VETERINARIO -> "Veterinario"
    Rol.ADMINISTRADOR -> "Administrador"
    else -> rol.orEmpty()
}
