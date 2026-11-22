package com.uth.proyectofinal.ui.vet

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
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.compose.viewModel
import com.uth.proyectofinal.data.remote.dto.Cita
import com.uth.proyectofinal.data.repository.ApiResult
import com.uth.proyectofinal.data.repository.CitaRepository
import com.uth.proyectofinal.ui.common.AppScaffold
import com.uth.proyectofinal.ui.common.EstadoBadge
import com.uth.proyectofinal.ui.common.ErrorBox
import com.uth.proyectofinal.ui.common.LoadingBox
import com.uth.proyectofinal.ui.common.OnResume
import com.uth.proyectofinal.ui.common.appFactory
import com.uth.proyectofinal.ui.common.lps
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class CitaDetailUi(
    val cita: Cita? = null,
    val cargando: Boolean = true,
    val error: String? = null,
)

class CitaDetailViewModel(private val id: Int, private val citas: CitaRepository) : ViewModel() {

    private val _ui = MutableStateFlow(CitaDetailUi())
    val ui: StateFlow<CitaDetailUi> = _ui.asStateFlow()

    fun cargar() {
        viewModelScope.launch {
            _ui.update { it.copy(cargando = true, error = null) }
            when (val result = citas.get(id)) {
                is ApiResult.Success -> _ui.update { it.copy(cita = result.data, cargando = false) }
                is ApiResult.Error -> _ui.update { it.copy(cargando = false, error = result.message) }
            }
        }
    }
}

@Composable
fun CitaDetailScreen(
    id: Int,
    onBack: () -> Unit,
    onConsulta: (Int) -> Unit,
    onVacuna: (Int, Int) -> Unit,
    onHistorialMascota: (Int) -> Unit,
) {
    val vm: CitaDetailViewModel = viewModel(factory = appFactory { CitaDetailViewModel(id, it.citas) })
    val ui by vm.ui.collectAsStateWithLifecycle()

    OnResume { vm.cargar() }

    AppScaffold(title = "Detalle de cita", onBack = onBack) { padding ->
        val cita = ui.cita
        when {
            ui.cargando && cita == null -> LoadingBox(Modifier.padding(padding))
            cita == null -> ErrorBox(ui.error ?: "No se pudo cargar la cita.", vm::cargar, Modifier.padding(padding))
            else -> Column(
                modifier = Modifier.padding(padding).padding(24.dp).fillMaxSize(),
                verticalArrangement = Arrangement.spacedBy(10.dp),
            ) {
                Text("${cita.fecha} · ${cita.hora}", style = MaterialTheme.typography.headlineSmall)
                EstadoBadge(cita.estado)
                Text("Mascota: ${cita.mascota.nombre}", style = MaterialTheme.typography.bodyLarge)
                Text("Dueño: ${cita.dueno?.nombre.orEmpty()}", style = MaterialTheme.typography.bodyLarge)
                Text("Servicio: ${cita.servicio.nombre} (${cita.servicio.precio.lps()})", style = MaterialTheme.typography.bodyLarge)
                Text("Veterinario: ${cita.veterinario.nombre}", style = MaterialTheme.typography.bodyLarge)

                Button(
                    onClick = { onHistorialMascota(cita.mascota.id) },
                    modifier = Modifier.fillMaxWidth(),
                ) {
                    Text("Ver historial de citas de la mascota")
                }
                Button(onClick = { onConsulta(cita.id) }, modifier = Modifier.fillMaxWidth()) {
                    Text("Registrar consulta")
                }
                OutlinedButton(
                    onClick = { onVacuna(cita.id, cita.mascota.id) },
                    modifier = Modifier.fillMaxWidth(),
                ) {
                    Text("Registrar vacuna")
                }
            }
        }
    }
}
