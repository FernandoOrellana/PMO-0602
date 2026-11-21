package com.uth.proyectofinal.ui.admin

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.uth.proyectofinal.data.remote.dto.Cita
import com.uth.proyectofinal.data.remote.dto.CitaEstado
import com.uth.proyectofinal.data.repository.ApiResult
import com.uth.proyectofinal.data.repository.CitaRepository
import com.uth.proyectofinal.data.repository.Page
import com.uth.proyectofinal.ui.common.*

class AdminCitasViewModel(private val citas: CitaRepository) : PagedViewModel<Cita>() {
    override suspend fun fetch(page: Int): ApiResult<Page<List<Cita>>> = citas.list(null, CitaEstado.ATENDIDA, page)
}

@Composable
fun AdminCitasScreen(onOpen: (Int) -> Unit) {
    val vm: AdminCitasViewModel = viewModel(factory = appFactory { AdminCitasViewModel(it.citas) })
    val state by vm.state.collectAsStateWithLifecycle()
    OnResume { vm.refresh() }
    AppScaffold(title = "Citas atendidas") { padding ->
        PagedColumn(state = state, emptyText = "No hay citas atendidas.", onLoadMore = vm::loadMore, modifier = Modifier.padding(padding)) { cita ->
            Card(onClick = { onOpen(cita.id) }, modifier = Modifier.fillMaxWidth()) {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                    Text("Cita #${cita.id} · ${cita.fecha} ${cita.hora}", style = MaterialTheme.typography.titleMedium)
                    Text("Cliente: ${cita.dueno?.nombre ?: "No disponible"}")
                    Text("Mascota: ${cita.mascota.nombre} · ${cita.servicio.nombre}")
                    EstadoBadge(cita.estado)
                }
            }
        }
    }
}
