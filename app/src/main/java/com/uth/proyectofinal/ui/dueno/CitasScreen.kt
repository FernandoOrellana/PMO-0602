package com.uth.proyectofinal.ui.dueno

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Card
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.compose.viewModel
import com.uth.proyectofinal.data.remote.dto.Cita
import com.uth.proyectofinal.data.remote.dto.CitaEstado
import com.uth.proyectofinal.data.repository.ApiResult
import com.uth.proyectofinal.data.repository.CitaRepository
import com.uth.proyectofinal.data.repository.Page
import com.uth.proyectofinal.ui.common.AddFab
import com.uth.proyectofinal.ui.common.AppScaffold
import com.uth.proyectofinal.ui.common.ConfirmDialog
import com.uth.proyectofinal.ui.common.EstadoBadge
import com.uth.proyectofinal.ui.common.OnResume
import com.uth.proyectofinal.ui.common.PagedColumn
import com.uth.proyectofinal.ui.common.PagedViewModel
import com.uth.proyectofinal.ui.common.appFactory
import com.uth.proyectofinal.ui.common.lps
import kotlinx.coroutines.launch

class CitasViewModel(private val repo: CitaRepository) : PagedViewModel<Cita>() {

    override suspend fun fetch(page: Int): ApiResult<Page<List<Cita>>> = repo.list(null, null, page)

    fun cancelar(id: Int) {
        viewModelScope.launch {
            when (val result = repo.cancelar(id)) {
                is ApiResult.Success -> {
                    say("Cita cancelada.")
                    refresh()
                }
                is ApiResult.Error -> say(result.message)
            }
        }
    }
}

@Composable
fun CitasScreen(onNueva: () -> Unit) {
    val vm: CitasViewModel = viewModel(factory = appFactory { CitasViewModel(it.citas) })
    val state by vm.state.collectAsStateWithLifecycle()
    var aCancelar by remember { mutableStateOf<Cita?>(null) }

    OnResume { vm.refresh() }

    AppScaffold(
        title = "Mis citas",
        messages = vm.messages,
        floatingActionButton = { AddFab("Agendar cita", onNueva) },
    ) { padding ->
        PagedColumn(
            state = state,
            emptyText = "No tienes citas. Usa + para agendar una.",
            onLoadMore = vm::loadMore, onRefresh = vm::pullRefresh,
            modifier = Modifier.padding(padding),
        ) { cita ->
            Card(modifier = Modifier.fillMaxWidth()) {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Text(
                            "${cita.fecha} ${cita.hora}",
                            style = MaterialTheme.typography.titleMedium,
                            modifier = Modifier.weight(1f),
                        )
                        EstadoBadge(cita.estado)
                    }
                    Text("${cita.mascota.nombre} · ${cita.servicio.nombre} (${cita.servicio.precio.lps()})")
                    Text("Veterinario: ${cita.veterinario.nombre}", style = MaterialTheme.typography.bodySmall)
                    if (cita.estado == CitaEstado.PENDIENTE || cita.estado == CitaEstado.CONFIRMADA) {
                        TextButton(onClick = { aCancelar = cita }) { Text("Cancelar cita") }
                    }
                }
            }
        }
    }

    aCancelar?.let { cita ->
        ConfirmDialog(
            title = "Cancelar cita",
            text = "¿Cancelar la cita de ${cita.mascota.nombre} el ${cita.fecha} a las ${cita.hora}?",
            confirmText = "Sí, cancelar",
            onConfirm = {
                aCancelar = null
                vm.cancelar(cita.id)
            },
            onDismiss = { aCancelar = null },
        )
    }
}
