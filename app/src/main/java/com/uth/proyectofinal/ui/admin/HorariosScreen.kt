package com.uth.proyectofinal.ui.admin

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Checkbox
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.compose.viewModel
import com.uth.proyectofinal.data.remote.dto.Horario
import com.uth.proyectofinal.data.repository.AdminRepository
import com.uth.proyectofinal.data.repository.ApiResult
import com.uth.proyectofinal.data.repository.CatalogRepository
import com.uth.proyectofinal.ui.common.ActionButton
import com.uth.proyectofinal.ui.common.AppScaffold
import com.uth.proyectofinal.ui.common.AppTextField
import com.uth.proyectofinal.ui.common.ErrorText
import com.uth.proyectofinal.ui.common.LoadingBox
import com.uth.proyectofinal.ui.common.appFactory
import com.uth.proyectofinal.ui.common.esHora
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

private val NOMBRES_DIAS = listOf("Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado")

data class DiaHorario(
    val diaSemana: Int,
    val activo: Boolean,
    val inicio: String,
    val fin: String,
)

data class HorariosUi(
    val nombre: String = "",
    val dias: List<DiaHorario> = emptyList(),
    val cargando: Boolean = true,
    val guardando: Boolean = false,
    val error: String? = null,
    val listo: Boolean = false,
)

class HorariosViewModel(
    private val id: Int,
    private val catalog: CatalogRepository,
    private val admin: AdminRepository,
) : ViewModel() {

    private val _ui = MutableStateFlow(HorariosUi())
    val ui: StateFlow<HorariosUi> = _ui.asStateFlow()

    init {
        viewModelScope.launch {
            when (val result = catalog.veterinarios()) {
                is ApiResult.Success -> {
                    val vet = result.data.find { it.id == id }
                    val dias = (0..6).map { d ->
                        val h = vet?.horarios?.find { it.diaSemana == d }
                        DiaHorario(d, h != null, h?.horaInicio ?: "08:00", h?.horaFin ?: "17:00")
                    }
                    _ui.update {
                        it.copy(
                            nombre = vet?.nombre.orEmpty(),
                            dias = dias,
                            cargando = false,
                            error = if (vet == null) "No se encontró el veterinario." else null,
                        )
                    }
                }
                is ApiResult.Error -> _ui.update { it.copy(cargando = false, error = result.message) }
            }
        }
    }

    fun actualizarDia(diaSemana: Int, transform: (DiaHorario) -> DiaHorario) {
        _ui.update { s ->
            s.copy(dias = s.dias.map { if (it.diaSemana == diaSemana) transform(it) else it }, error = null)
        }
    }

    fun guardar() {
        val dias = _ui.value.dias
        val activos = dias.filter { it.activo }
        val invalido = activos.firstOrNull { !it.inicio.esHora() || !it.fin.esHora() || it.inicio >= it.fin }
        if (invalido != null) {
            _ui.update {
                it.copy(error = "Revisa el horario de ${NOMBRES_DIAS[invalido.diaSemana]}: use HH:mm y hora inicio menor que fin.")
            }
            return
        }
        val horarios = activos.map { Horario(it.diaSemana, it.inicio, it.fin) }
        viewModelScope.launch {
            _ui.update { it.copy(guardando = true, error = null) }
            when (val result = admin.guardarHorarios(id, horarios)) {
                is ApiResult.Success -> _ui.update { it.copy(guardando = false, listo = true) }
                is ApiResult.Error -> _ui.update { it.copy(guardando = false, error = result.message) }
            }
        }
    }
}

@Composable
fun HorariosScreen(id: Int, onBack: () -> Unit) {
    val vm: HorariosViewModel = viewModel(factory = appFactory { HorariosViewModel(id, it.catalog, it.admin) })
    val ui by vm.ui.collectAsStateWithLifecycle()

    LaunchedEffect(ui.listo) {
        if (ui.listo) onBack()
    }

    AppScaffold(title = "Horarios de ${ui.nombre}".trim(), onBack = onBack) { padding ->
        if (ui.cargando) {
            LoadingBox(Modifier.padding(padding))
        } else {
            Column(
                modifier = Modifier
                    .padding(padding)
                    .padding(16.dp)
                    .fillMaxSize()
                    .verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(10.dp),
            ) {
                ui.dias.forEach { dia ->
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                    ) {
                        Checkbox(
                            checked = dia.activo,
                            onCheckedChange = { vm.actualizarDia(dia.diaSemana) { d -> d.copy(activo = it) } },
                        )
                        Text(NOMBRES_DIAS[dia.diaSemana], modifier = Modifier.width(90.dp))
                        if (dia.activo) {
                            AppTextField(
                                value = dia.inicio,
                                onValueChange = { v -> vm.actualizarDia(dia.diaSemana) { it.copy(inicio = v) } },
                                label = "Inicio",
                                modifier = Modifier.weight(1f),
                            )
                            AppTextField(
                                value = dia.fin,
                                onValueChange = { v -> vm.actualizarDia(dia.diaSemana) { it.copy(fin = v) } },
                                label = "Fin",
                                modifier = Modifier.weight(1f),
                            )
                        }
                    }
                }
                ErrorText(ui.error)
                ActionButton("Guardar horarios", ui.guardando, vm::guardar)
            }
        }
    }
}
