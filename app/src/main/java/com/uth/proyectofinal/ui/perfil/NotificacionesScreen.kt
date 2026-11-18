package com.uth.proyectofinal.ui.perfil

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Card
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.compose.viewModel
import com.uth.proyectofinal.data.remote.dto.Notificacion
import com.uth.proyectofinal.data.repository.ApiResult
import com.uth.proyectofinal.data.repository.NotificacionRepository
import com.uth.proyectofinal.ui.common.AppScaffold
import com.uth.proyectofinal.ui.common.OnResume
import com.uth.proyectofinal.ui.common.PagedColumn
import com.uth.proyectofinal.ui.common.PagedState
import com.uth.proyectofinal.ui.common.appFactory
import com.uth.proyectofinal.ui.common.textoFecha
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

class NotificacionesViewModel(private val repo: NotificacionRepository) : ViewModel() {

    private val _state = MutableStateFlow(PagedState<Notificacion>(hasMore = false))
    val state: StateFlow<PagedState<Notificacion>> = _state.asStateFlow()

    fun cargar() {
        viewModelScope.launch {
            _state.update { it.copy(loading = true, error = null) }
            when (val result = repo.list()) {
                is ApiResult.Success -> _state.update { it.copy(items = result.data, loading = false) }
                is ApiResult.Error -> _state.update { it.copy(loading = false, error = result.message) }
            }
        }
    }

    fun marcarLeida(id: Int) {
        viewModelScope.launch {
            if (repo.marcarLeida(id) is ApiResult.Success) {
                _state.update { s -> s.copy(items = s.items.map { if (it.id == id) it.copy(leida = true) else it }) }
            }
        }
    }
}

@Composable
fun NotificacionesScreen(onBack: () -> Unit) {
    val vm: NotificacionesViewModel = viewModel(factory = appFactory { NotificacionesViewModel(it.notificaciones) })
    val state by vm.state.collectAsStateWithLifecycle()

    OnResume { vm.cargar() }

    AppScaffold(title = "Notificaciones", onBack = onBack) { padding ->
        PagedColumn(
            state = state,
            emptyText = "No tienes notificaciones.",
            onLoadMore = vm::cargar,
            modifier = Modifier.padding(padding),
        ) { n ->
            Card(modifier = Modifier.fillMaxWidth()) {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                    Text(
                        n.titulo,
                        style = MaterialTheme.typography.titleMedium,
                        color = if (n.leida) MaterialTheme.colorScheme.onSurfaceVariant else MaterialTheme.colorScheme.primary,
                    )
                    Text(n.mensaje, style = MaterialTheme.typography.bodyMedium)
                    Text(n.fecha.textoFecha(), style = MaterialTheme.typography.labelSmall)
                    if (!n.leida) {
                        TextButton(onClick = { vm.marcarLeida(n.id) }) { Text("Marcar como leída") }
                    }
                }
            }
        }
    }
}
