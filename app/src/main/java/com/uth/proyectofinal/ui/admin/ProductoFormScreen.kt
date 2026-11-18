package com.uth.proyectofinal.ui.admin

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.PickVisualMediaRequest
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.compose.viewModel
import coil3.compose.AsyncImage
import com.uth.proyectofinal.data.remote.dto.ProductoRequest
import com.uth.proyectofinal.data.repository.AdminRepository
import com.uth.proyectofinal.data.repository.ApiResult
import com.uth.proyectofinal.data.repository.TiendaRepository
import com.uth.proyectofinal.ui.common.ActionButton
import com.uth.proyectofinal.ui.common.AppScaffold
import com.uth.proyectofinal.ui.common.AppTextField
import com.uth.proyectofinal.ui.common.ErrorText
import com.uth.proyectofinal.ui.common.LoadingBox
import com.uth.proyectofinal.ui.common.MessageViewModel
import com.uth.proyectofinal.ui.common.appFactory
import com.uth.proyectofinal.ui.common.imagenPart
import com.uth.proyectofinal.ui.common.opcional
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class ProductoFormUi(
    val nombre: String = "",
    val descripcion: String = "",
    val categoria: String = "",
    val precio: String = "",
    val stock: String = "",
    val imagenUrl: String? = null,
    val cargando: Boolean = false,
    val guardando: Boolean = false,
    val subiendoImagen: Boolean = false,
    val error: String? = null,
    val listo: Boolean = false,
)

class ProductoFormViewModel(
    private val id: Int,
    private val tienda: TiendaRepository,
    private val admin: AdminRepository,
) : MessageViewModel() {

    private val _ui = MutableStateFlow(ProductoFormUi(cargando = id > 0))
    val ui: StateFlow<ProductoFormUi> = _ui.asStateFlow()

    init {
        if (id > 0) {
            viewModelScope.launch {
                when (val result = tienda.producto(id)) {
                    is ApiResult.Success -> _ui.update {
                        it.copy(
                            cargando = false,
                            nombre = result.data.nombre,
                            descripcion = result.data.descripcion.orEmpty(),
                            categoria = result.data.categoria,
                            precio = result.data.precio.toString(),
                            stock = result.data.stock.toString(),
                            imagenUrl = result.data.imagenUrl,
                        )
                    }
                    is ApiResult.Error -> _ui.update { it.copy(cargando = false, error = result.message) }
                }
            }
        }
    }

    fun onNombre(value: String) = _ui.update { it.copy(nombre = value, error = null) }
    fun onDescripcion(value: String) = _ui.update { it.copy(descripcion = value, error = null) }
    fun onCategoria(value: String) = _ui.update { it.copy(categoria = value, error = null) }
    fun onPrecio(value: String) = _ui.update { it.copy(precio = value, error = null) }
    fun onStock(value: String) = _ui.update { it.copy(stock = value, error = null) }

    fun guardar() {
        val s = _ui.value
        val precio = s.precio.trim().toDoubleOrNull()
        val stock = s.stock.trim().toIntOrNull()
        val validation = when {
            s.nombre.isBlank() -> "Ingresa el nombre del producto."
            s.categoria.isBlank() -> "Ingresa la categoría."
            precio == null || precio < 0 -> "El precio debe ser un número válido."
            stock == null || stock < 0 -> "El stock debe ser un número entero."
            else -> null
        }
        if (validation != null || precio == null || stock == null) {
            _ui.update { it.copy(error = validation) }
            return
        }
        val body = ProductoRequest(
            nombre = s.nombre.trim(),
            descripcion = s.descripcion.opcional(),
            categoria = s.categoria.trim(),
            precio = precio,
            stock = stock,
        )
        viewModelScope.launch {
            _ui.update { it.copy(guardando = true, error = null) }
            val result = if (id > 0) admin.editarProducto(id, body) else admin.crearProducto(body)
            when (result) {
                is ApiResult.Success -> _ui.update { it.copy(guardando = false, listo = true) }
                is ApiResult.Error -> _ui.update { it.copy(guardando = false, error = result.message) }
            }
        }
    }

    fun subirImagen(part: okhttp3.MultipartBody.Part) {
        viewModelScope.launch {
            _ui.update { it.copy(subiendoImagen = true) }
            when (val result = admin.subirImagenProducto(id, part)) {
                is ApiResult.Success -> {
                    _ui.update { it.copy(imagenUrl = result.data.imagenUrl) }
                    say("Imagen actualizada.")
                }
                is ApiResult.Error -> say(result.message)
            }
            _ui.update { it.copy(subiendoImagen = false) }
        }
    }
}

@Composable
fun ProductoFormScreen(id: Int, onBack: () -> Unit) {
    val vm: ProductoFormViewModel = viewModel(
        factory = appFactory { ProductoFormViewModel(id, it.tienda, it.admin) },
    )
    val ui by vm.ui.collectAsStateWithLifecycle()
    val context = LocalContext.current
    val scope = rememberCoroutineScope()

    val picker = rememberLauncherForActivityResult(ActivityResultContracts.PickVisualMedia()) { uri ->
        if (uri != null) scope.launch { vm.subirImagen(context.imagenPart(uri, "imagen")) }
    }

    LaunchedEffect(ui.listo) {
        if (ui.listo) onBack()
    }

    AppScaffold(
        title = if (id > 0) "Editar producto" else "Nuevo producto",
        onBack = onBack,
        messages = vm.messages,
    ) { padding ->
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
                if (id > 0) {
                    AsyncImage(
                        model = ui.imagenUrl,
                        contentDescription = ui.nombre,
                        contentScale = ContentScale.Crop,
                        modifier = Modifier.fillMaxWidth().height(160.dp),
                    )
                    OutlinedButton(
                        onClick = {
                            picker.launch(PickVisualMediaRequest(ActivityResultContracts.PickVisualMedia.ImageOnly))
                        },
                        enabled = !ui.subiendoImagen,
                        modifier = Modifier.fillMaxWidth(),
                    ) { Text(if (ui.subiendoImagen) "Subiendo imagen..." else "Cambiar imagen") }
                }
                AppTextField(ui.nombre, vm::onNombre, "Nombre")
                AppTextField(ui.descripcion, vm::onDescripcion, "Descripción (opcional)", singleLine = false)
                AppTextField(ui.categoria, vm::onCategoria, "Categoría")
                AppTextField(ui.precio, vm::onPrecio, "Precio (Lps.)", keyboard = KeyboardType.Decimal)
                AppTextField(ui.stock, vm::onStock, "Stock", keyboard = KeyboardType.Number)
                ErrorText(ui.error)
                ActionButton("Guardar", ui.guardando, vm::guardar)
            }
        }
    }
}
