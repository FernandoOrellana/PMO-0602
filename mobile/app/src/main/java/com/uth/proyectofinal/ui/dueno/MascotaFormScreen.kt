package com.uth.proyectofinal.ui.dueno

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.compose.viewModel
import com.uth.proyectofinal.data.remote.dto.MascotaRequest
import com.uth.proyectofinal.data.repository.ApiResult
import com.uth.proyectofinal.data.repository.CatalogRepository
import com.uth.proyectofinal.data.repository.MascotaRepository
import com.uth.proyectofinal.ui.common.ActionButton
import com.uth.proyectofinal.ui.common.AppScaffold
import com.uth.proyectofinal.ui.common.AppTextField
import com.uth.proyectofinal.ui.common.ErrorText
import com.uth.proyectofinal.ui.common.LoadingBox
import com.uth.proyectofinal.ui.common.Option
import com.uth.proyectofinal.ui.common.SelectField
import com.uth.proyectofinal.ui.common.appFactory
import com.uth.proyectofinal.ui.common.esFecha
import com.uth.proyectofinal.ui.common.opcional
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class MascotaFormUi(
    val nombre: String = "",
    val especieId: Int? = null,
    val raza: String = "",
    val fechaNacimiento: String = "",
    val peso: String = "",
    val especies: List<Option> = emptyList(),
    val cargando: Boolean = true,
    val guardando: Boolean = false,
    val error: String? = null,
    val listo: Boolean = false,
)

class MascotaFormViewModel(
    private val id: Int,
    private val mascotas: MascotaRepository,
    private val catalog: CatalogRepository,
) : ViewModel() {

    private val _ui = MutableStateFlow(MascotaFormUi())
    val ui: StateFlow<MascotaFormUi> = _ui.asStateFlow()

    init {
        cargar()
    }

    fun onNombre(value: String) = _ui.update { it.copy(nombre = value, error = null) }
    fun onEspecie(value: Int) = _ui.update { it.copy(especieId = value, error = null) }
    fun onRaza(value: String) = _ui.update { it.copy(raza = value, error = null) }
    fun onFecha(value: String) = _ui.update { it.copy(fechaNacimiento = value, error = null) }
    fun onPeso(value: String) = _ui.update { it.copy(peso = value, error = null) }

    private fun cargar() {
        viewModelScope.launch {
            _ui.update { it.copy(cargando = true, error = null) }
            when (val especies = catalog.especies()) {
                is ApiResult.Error -> _ui.update { it.copy(cargando = false, error = especies.message) }
                is ApiResult.Success -> {
                    val opciones = especies.data.map { Option(it.id, it.nombre) }
                    if (id == 0) {
                        _ui.update { it.copy(especies = opciones, cargando = false) }
                    } else {
                        when (val m = mascotas.get(id)) {
                            is ApiResult.Error -> _ui.update { it.copy(especies = opciones, cargando = false, error = m.message) }
                            is ApiResult.Success -> _ui.update {
                                it.copy(
                                    especies = opciones,
                                    cargando = false,
                                    nombre = m.data.nombre,
                                    especieId = m.data.especie?.id,
                                    raza = m.data.raza.orEmpty(),
                                    fechaNacimiento = m.data.fechaNacimiento.orEmpty().take(10),
                                    peso = m.data.pesoKg?.toString().orEmpty(),
                                )
                            }
                        }
                    }
                }
            }
        }
    }

    fun guardar() {
        val s = _ui.value
        val fecha = s.fechaNacimiento.opcional()
        val peso = s.peso.opcional()?.toDoubleOrNull()
        val especieId = s.especieId
        val validation = when {
            s.nombre.isBlank() -> "Ingresa el nombre de la mascota."
            especieId == null -> "Selecciona la especie."
            fecha != null && !fecha.esFecha() -> "La fecha debe tener formato AAAA-MM-DD."
            s.peso.isNotBlank() && peso == null -> "El peso debe ser un número."
            else -> null
        }
        if (validation != null || especieId == null) {
            _ui.update { it.copy(error = validation) }
            return
        }
        val body = MascotaRequest(s.nombre.trim(), especieId, s.raza.opcional(), fecha, peso)
        viewModelScope.launch {
            _ui.update { it.copy(guardando = true, error = null) }
            val result = if (id == 0) mascotas.create(body) else mascotas.update(id, body)
            when (result) {
                is ApiResult.Success -> _ui.update { it.copy(guardando = false, listo = true) }
                is ApiResult.Error -> _ui.update { it.copy(guardando = false, error = result.message) }
            }
        }
    }
}

@Composable
fun MascotaFormScreen(id: Int, onBack: () -> Unit, onSaved: () -> Unit) {
    val vm: MascotaFormViewModel = viewModel(
        factory = appFactory { MascotaFormViewModel(id, it.mascotas, it.catalog) },
    )
    val ui by vm.ui.collectAsStateWithLifecycle()

    LaunchedEffect(ui.listo) {
        if (ui.listo) onSaved()
    }

    AppScaffold(title = if (id == 0) "Nueva mascota" else "Editar mascota", onBack = onBack) { padding ->
        if (ui.cargando) {
            LoadingBox(Modifier.padding(padding))
        } else {
            Column(
                modifier = Modifier
                    .padding(padding)
                    .padding(24.dp)
                    .fillMaxSize()
                    .verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                AppTextField(ui.nombre, vm::onNombre, "Nombre")
                SelectField("Especie", ui.especies, ui.especieId, vm::onEspecie)
                AppTextField(ui.raza, vm::onRaza, "Raza (opcional)")
                AppTextField(ui.fechaNacimiento, vm::onFecha, "Fecha de nacimiento (AAAA-MM-DD)")
                AppTextField(ui.peso, vm::onPeso, "Peso en kg (opcional)", keyboard = KeyboardType.Decimal)
                ErrorText(ui.error)
                ActionButton("Guardar", ui.guardando, vm::guardar)
            }
        }
    }
}
