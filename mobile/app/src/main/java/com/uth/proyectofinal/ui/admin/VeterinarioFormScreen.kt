package com.uth.proyectofinal.ui.admin

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
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
import com.uth.proyectofinal.data.remote.dto.CrearVeterinarioRequest
import com.uth.proyectofinal.data.remote.dto.EditarVeterinarioRequest
import com.uth.proyectofinal.data.repository.AdminRepository
import com.uth.proyectofinal.data.repository.ApiResult
import com.uth.proyectofinal.data.repository.CatalogRepository
import com.uth.proyectofinal.data.repository.map
import com.uth.proyectofinal.ui.common.ActionButton
import com.uth.proyectofinal.ui.common.AppScaffold
import com.uth.proyectofinal.ui.common.AppTextField
import com.uth.proyectofinal.ui.common.ErrorText
import com.uth.proyectofinal.ui.common.LoadingBox
import com.uth.proyectofinal.ui.common.appFactory
import com.uth.proyectofinal.ui.common.opcional
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class VeterinarioFormUi(
    val nombre: String = "",
    val email: String = "",
    val password: String = "",
    val telefono: String = "",
    val cargando: Boolean = false,
    val guardando: Boolean = false,
    val error: String? = null,
    val listo: Boolean = false,
)

class VeterinarioFormViewModel(
    private val id: Int,
    private val catalog: CatalogRepository,
    private val admin: AdminRepository,
) : ViewModel() {

    private val _ui = MutableStateFlow(VeterinarioFormUi(cargando = id > 0))
    val ui: StateFlow<VeterinarioFormUi> = _ui.asStateFlow()

    init {
        if (id > 0) {
            viewModelScope.launch {
                when (val result = catalog.veterinarios()) {
                    is ApiResult.Success -> {
                        val vet = result.data.find { it.id == id }
                        _ui.update {
                            it.copy(
                                cargando = false,
                                nombre = vet?.nombre.orEmpty(),
                                email = vet?.email.orEmpty(),
                                telefono = vet?.telefono.orEmpty(),
                                error = if (vet == null) "No se encontró el veterinario." else null,
                            )
                        }
                    }
                    is ApiResult.Error -> _ui.update { it.copy(cargando = false, error = result.message) }
                }
            }
        }
    }

    fun onNombre(value: String) = _ui.update { it.copy(nombre = value, error = null) }
    fun onEmail(value: String) = _ui.update { it.copy(email = value, error = null) }
    fun onPassword(value: String) = _ui.update { it.copy(password = value, error = null) }
    fun onTelefono(value: String) = _ui.update { it.copy(telefono = value, error = null) }

    fun guardar() {
        val s = _ui.value
        val validation = when {
            s.nombre.isBlank() -> "Ingresa el nombre."
            id == 0 && !s.email.contains("@") -> "Ingresa un correo válido."
            id == 0 && s.password.length < 8 -> "La contraseña debe tener al menos 8 caracteres."
            else -> null
        }
        if (validation != null) {
            _ui.update { it.copy(error = validation) }
            return
        }
        viewModelScope.launch {
            _ui.update { it.copy(guardando = true, error = null) }
            val result = if (id == 0) {
                admin.crearVeterinario(
                    CrearVeterinarioRequest(
                        nombre = s.nombre.trim(),
                        email = s.email.trim(),
                        password = s.password,
                        telefono = s.telefono.opcional(),
                    ),
                ).map { }
            } else {
                admin.editarVeterinario(id, EditarVeterinarioRequest(s.nombre.trim(), s.telefono.opcional())).map { }
            }
            when (result) {
                is ApiResult.Success -> _ui.update { it.copy(guardando = false, listo = true) }
                is ApiResult.Error -> _ui.update { it.copy(guardando = false, error = result.message) }
            }
        }
    }
}

@Composable
fun VeterinarioFormScreen(id: Int, onBack: () -> Unit) {
    val vm: VeterinarioFormViewModel = viewModel(
        factory = appFactory { VeterinarioFormViewModel(id, it.catalog, it.admin) },
    )
    val ui by vm.ui.collectAsStateWithLifecycle()

    LaunchedEffect(ui.listo) {
        if (ui.listo) onBack()
    }

    AppScaffold(title = if (id == 0) "Nuevo veterinario" else "Editar veterinario", onBack = onBack) { padding ->
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
                if (id == 0) {
                    AppTextField(ui.email, vm::onEmail, "Correo electrónico", keyboard = KeyboardType.Email)
                    AppTextField(ui.password, vm::onPassword, "Contraseña inicial (mínimo 8)", password = true)
                } else {
                    Text("Correo: ${ui.email}")
                }
                AppTextField(ui.telefono, vm::onTelefono, "Teléfono (opcional)", keyboard = KeyboardType.Phone)
                ErrorText(ui.error)
                ActionButton("Guardar", ui.guardando, vm::guardar)
            }
        }
    }
}
