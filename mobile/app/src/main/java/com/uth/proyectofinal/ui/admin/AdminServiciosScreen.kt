package com.uth.proyectofinal.ui.admin

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.AlertDialog
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
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.compose.viewModel
import com.uth.proyectofinal.data.remote.dto.Servicio
import com.uth.proyectofinal.data.remote.dto.ServicioRequest
import com.uth.proyectofinal.data.repository.AdminRepository
import com.uth.proyectofinal.data.repository.ApiResult
import com.uth.proyectofinal.data.repository.CatalogRepository
import com.uth.proyectofinal.ui.common.AddFab
import com.uth.proyectofinal.ui.common.AppScaffold
import com.uth.proyectofinal.ui.common.AppTextField
import com.uth.proyectofinal.ui.common.ConfirmDialog
import com.uth.proyectofinal.ui.common.DeleteIcon
import com.uth.proyectofinal.ui.common.EditIcon
import com.uth.proyectofinal.ui.common.EmptyBox
import com.uth.proyectofinal.ui.common.ErrorBox
import com.uth.proyectofinal.ui.common.ErrorText
import com.uth.proyectofinal.ui.common.LoadingBox
import com.uth.proyectofinal.ui.common.MessageViewModel
import com.uth.proyectofinal.ui.common.OnResume
import com.uth.proyectofinal.ui.common.appFactory
import com.uth.proyectofinal.ui.common.lps
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class ServicioForm(
    val id: Int? = null,
    val nombre: String = "",
    val precio: String = "",
    val duracion: String = "",
    val guardando: Boolean = false,
    val error: String? = null,
)

data class ServiciosUi(
    val items: List<Servicio> = emptyList(),
    val cargando: Boolean = false,
    val error: String? = null,
    val form: ServicioForm? = null,
)

class AdminServiciosViewModel(
    private val catalog: CatalogRepository,
    private val admin: AdminRepository,
) : MessageViewModel() {

    private val _ui = MutableStateFlow(ServiciosUi())
    val ui: StateFlow<ServiciosUi> = _ui.asStateFlow()

    fun cargar() {
        viewModelScope.launch {
            _ui.update { it.copy(cargando = true, error = null) }
            when (val result = catalog.servicios()) {
                is ApiResult.Success -> _ui.update { it.copy(items = result.data, cargando = false) }
                is ApiResult.Error -> _ui.update { it.copy(cargando = false, error = result.message) }
            }
        }
    }

    fun abrirNuevo() = _ui.update { it.copy(form = ServicioForm()) }

    fun abrirEditar(s: Servicio) = _ui.update {
        it.copy(form = ServicioForm(id = s.id, nombre = s.nombre, precio = s.precio.toString(), duracion = s.duracionMin.toString()))
    }

    fun cerrarForm() = _ui.update { it.copy(form = null) }

    fun editarForm(transform: (ServicioForm) -> ServicioForm) = _ui.update { state ->
        state.copy(form = state.form?.let(transform))
    }

    fun guardar() {
        val f = _ui.value.form ?: return
        val precio = f.precio.trim().toDoubleOrNull()
        val duracion = f.duracion.trim().toIntOrNull()
        val validation = when {
            f.nombre.isBlank() -> "Ingresa el nombre del servicio."
            precio == null || precio < 0 -> "El precio debe ser un número válido."
            duracion == null || duracion <= 0 -> "La duración debe ser un número de minutos."
            else -> null
        }
        if (validation != null || precio == null || duracion == null) {
            editarForm { it.copy(error = validation) }
            return
        }
        val body = ServicioRequest(f.nombre.trim(), precio, duracion)
        viewModelScope.launch {
            editarForm { it.copy(guardando = true, error = null) }
            val result = f.id?.let { admin.editarServicio(it, body) } ?: admin.crearServicio(body)
            when (result) {
                is ApiResult.Success -> {
                    cerrarForm()
                    say("Servicio guardado.")
                    cargar()
                }
                is ApiResult.Error -> editarForm { it.copy(guardando = false, error = result.message) }
            }
        }
    }

    fun eliminar(id: Int) {
        viewModelScope.launch {
            when (val result = admin.eliminarServicio(id)) {
                is ApiResult.Success -> {
                    say("Servicio eliminado.")
                    cargar()
                }
                is ApiResult.Error -> say(result.message)
            }
        }
    }
}

@Composable
fun AdminServiciosScreen() {
    val vm: AdminServiciosViewModel = viewModel(
        factory = appFactory { AdminServiciosViewModel(it.catalog, it.admin) },
    )
    val ui by vm.ui.collectAsStateWithLifecycle()
    var aEliminar by remember { mutableStateOf<Servicio?>(null) }

    OnResume { vm.cargar() }

    AppScaffold(
        title = "Servicios",
        messages = vm.messages,
        floatingActionButton = { AddFab("Nuevo servicio", vm::abrirNuevo) },
    ) { padding ->
        when {
            ui.cargando && ui.items.isEmpty() -> LoadingBox(Modifier.padding(padding))
            ui.error != null && ui.items.isEmpty() -> ErrorBox(ui.error!!, vm::cargar, Modifier.padding(padding))
            ui.items.isEmpty() -> EmptyBox("No hay servicios. Usa + para crear uno.", Modifier.padding(padding))
            else -> LazyColumn(
                modifier = Modifier.padding(padding).fillMaxSize(),
                contentPadding = androidx.compose.foundation.layout.PaddingValues(16.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                items(ui.items, key = { it.id }) { s ->
                    Card(modifier = Modifier.fillMaxWidth()) {
                        Row(
                            modifier = Modifier.padding(12.dp).fillMaxWidth(),
                            verticalAlignment = Alignment.CenterVertically,
                        ) {
                            Column(Modifier.weight(1f).padding(start = 4.dp)) {
                                Text(s.nombre, style = MaterialTheme.typography.titleMedium)
                                Text("${s.precio.lps()} · ${s.duracionMin} min", style = MaterialTheme.typography.bodySmall)
                            }
                            EditIcon("Editar servicio") { vm.abrirEditar(s) }
                            DeleteIcon("Eliminar servicio") { aEliminar = s }
                        }
                    }
                }
            }
        }
    }

    ui.form?.let { form ->
        AlertDialog(
            onDismissRequest = vm::cerrarForm,
            title = { Text(if (form.id == null) "Nuevo servicio" else "Editar servicio") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    AppTextField(form.nombre, { v -> vm.editarForm { it.copy(nombre = v, error = null) } }, "Nombre")
                    AppTextField(
                        form.precio,
                        { v -> vm.editarForm { it.copy(precio = v, error = null) } },
                        "Precio (Lps.)",
                        keyboard = KeyboardType.Decimal,
                    )
                    AppTextField(
                        form.duracion,
                        { v -> vm.editarForm { it.copy(duracion = v, error = null) } },
                        "Duración (minutos)",
                        keyboard = KeyboardType.Number,
                    )
                    ErrorText(form.error)
                }
            },
            confirmButton = {
                TextButton(onClick = vm::guardar, enabled = !form.guardando) { Text("Guardar") }
            },
            dismissButton = { TextButton(onClick = vm::cerrarForm) { Text("Cancelar") } },
        )
    }

    aEliminar?.let { s ->
        ConfirmDialog(
            title = "Eliminar servicio",
            text = "¿Eliminar el servicio ${s.nombre}?",
            confirmText = "Eliminar",
            onConfirm = {
                aEliminar = null
                vm.eliminar(s.id)
            },
            onDismiss = { aEliminar = null },
        )
    }
}
