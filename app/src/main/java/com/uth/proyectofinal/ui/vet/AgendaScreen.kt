package com.uth.proyectofinal.ui.vet

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Card
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
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
import com.uth.proyectofinal.data.repository.ApiResult
import com.uth.proyectofinal.data.repository.CitaRepository
import com.uth.proyectofinal.data.repository.Page
import com.uth.proyectofinal.ui.common.AppScaffold
import com.uth.proyectofinal.ui.common.EstadoBadge
import com.uth.proyectofinal.ui.common.OnResume
import com.uth.proyectofinal.ui.common.PagedColumn
import com.uth.proyectofinal.ui.common.PagedViewModel
import com.uth.proyectofinal.ui.common.appFactory
import com.uth.proyectofinal.ui.common.esFecha
import com.uth.proyectofinal.ui.common.isoFecha
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

class AgendaViewModel(private val citas: CitaRepository) : PagedViewModel<Cita>() {

    private val _fecha = MutableStateFlow(isoFecha())
    val fecha: StateFlow<String> = _fecha.asStateFlow()

    override suspend fun fetch(page: Int): ApiResult<Page<List<Cita>>> =
        citas.list(_fecha.value, null, page, 50)

    fun cambiarFecha(nueva: String) {
        _fecha.value = nueva
        reload()
    }
}

@Composable
fun AgendaScreen(onOpen: (Int) -> Unit) {
    val vm: AgendaViewModel = viewModel(factory = appFactory { AgendaViewModel(it.citas) })
    val state by vm.state.collectAsStateWithLifecycle()
    val fecha by vm.fecha.collectAsStateWithLifecycle()
    var texto by remember(fecha) { mutableStateOf(fecha) }

    OnResume { vm.refresh() }

    AppScaffold(title = "Agenda de hoy", messages = vm.messages) { padding ->
        Column(Modifier.padding(padding)) {
            Column(Modifier.fillMaxWidth().padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                Text("Agenda del $fecha", style = MaterialTheme.typography.titleMedium)
                Row(verticalAlignment = Alignment.CenterVertically) {
                    OutlinedTextField(
                        value = texto,
                        onValueChange = { texto = it },
                        label = { Text("Fecha (AAAA-MM-DD)") },
                        singleLine = true,
                        modifier = Modifier.weight(1f),
                    )
                    TextButton(onClick = { if (texto.esFecha()) vm.cambiarFecha(texto) }) { Text("Ver") }
                }
                OutlinedButton(onClick = { vm.cambiarFecha(isoFecha()) }) { Text("Hoy") }
            }
            PagedColumn(
                state = state,
                emptyText = "No hay citas para esta fecha.",
                onLoadMore = vm::loadMore, onRefresh = vm::pullRefresh,
            ) { cita ->
                Card(onClick = { onOpen(cita.id) }, modifier = Modifier.fillMaxWidth()) {
                    Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Text(cita.hora, style = MaterialTheme.typography.titleMedium, modifier = Modifier.weight(1f))
                            EstadoBadge(cita.estado)
                        }
                        Text("Mascota: ${cita.mascota.nombre} (dueño: ${cita.dueno?.nombre.orEmpty()})")
                        Text("Servicio: ${cita.servicio.nombre}", style = MaterialTheme.typography.bodySmall)
                    }
                }
            }
        }
    }
}
