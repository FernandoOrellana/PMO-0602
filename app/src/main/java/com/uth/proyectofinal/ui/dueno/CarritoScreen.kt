package com.uth.proyectofinal.ui.dueno

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Card
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.compose.viewModel
import com.uth.proyectofinal.data.local.CartItem
import com.uth.proyectofinal.data.local.CartStore
import com.uth.proyectofinal.data.remote.dto.ItemPedidoRequest
import com.uth.proyectofinal.data.repository.ApiResult
import com.uth.proyectofinal.data.repository.TiendaRepository
import com.uth.proyectofinal.ui.common.ActionButton
import com.uth.proyectofinal.ui.common.AppScaffold
import com.uth.proyectofinal.ui.common.EmptyBox
import com.uth.proyectofinal.ui.common.ErrorText
import com.uth.proyectofinal.ui.common.appFactory
import com.uth.proyectofinal.ui.common.lps
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class CarritoUi(
    val enviando: Boolean = false,
    val error: String? = null,
    val pedidoCreado: Int? = null,
)

class CarritoViewModel(
    private val cart: CartStore,
    private val tienda: TiendaRepository,
) : ViewModel() {

    val items: StateFlow<List<CartItem>> = cart.items

    val total: StateFlow<Double> = cart.items
        .map { list -> list.sumOf { it.precio * it.cantidad } }
        .stateIn(viewModelScope, SharingStarted.Eagerly, 0.0)

    private val _ui = MutableStateFlow(CarritoUi())
    val ui: StateFlow<CarritoUi> = _ui.asStateFlow()

    fun cambiarCantidad(productoId: Int, cantidad: Int) = cart.setCantidad(productoId, cantidad)

    fun quitar(productoId: Int) = cart.setCantidad(productoId, 0)

    fun confirmar() {
        val request = cart.items.value.map { ItemPedidoRequest(it.productoId, it.cantidad) }
        if (request.isEmpty()) {
            _ui.update { it.copy(error = "Tu carrito está vacío.") }
            return
        }
        viewModelScope.launch {
            _ui.update { it.copy(enviando = true, error = null) }
            when (val result = tienda.crearPedido(request)) {
                is ApiResult.Success -> {
                    cart.clear()
                    _ui.update { it.copy(enviando = false, pedidoCreado = result.data.id) }
                }
                is ApiResult.Error -> _ui.update { it.copy(enviando = false, error = result.message) }
            }
        }
    }
}

@Composable
fun CarritoScreen(onBack: () -> Unit, onPedidoCreado: () -> Unit) {
    val vm: CarritoViewModel = viewModel(factory = appFactory { CarritoViewModel(it.cart, it.tienda) })
    val items by vm.items.collectAsStateWithLifecycle()
    val total by vm.total.collectAsStateWithLifecycle()
    val ui by vm.ui.collectAsStateWithLifecycle()

    AppScaffold(title = "Carrito", onBack = onBack) { padding ->
        if (items.isEmpty()) {
            EmptyBox("Tu carrito está vacío.", Modifier.padding(padding))
        } else {
            Column(Modifier.padding(padding).fillMaxSize()) {
                LazyColumn(
                    modifier = Modifier.weight(1f),
                    contentPadding = androidx.compose.foundation.layout.PaddingValues(16.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    items(items, key = { it.productoId }) { item ->
                        CartRow(item, vm)
                    }
                }
                Column(Modifier.fillMaxWidth().padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text("Total: ${total.lps()}", style = MaterialTheme.typography.titleLarge)
                    Text("Pague en mostrador al recoger su pedido.", style = MaterialTheme.typography.bodyMedium)
                    ErrorText(ui.error)
                    ActionButton("Confirmar pedido", ui.enviando, vm::confirmar)
                }
            }
        }
    }

    val creado = ui.pedidoCreado
    if (creado != null) {
        AlertDialog(
            onDismissRequest = onPedidoCreado,
            title = { Text("Pedido registrado") },
            text = { Text("Pedido #$creado registrado. Pague en mostrador al recoger su pedido.") },
            confirmButton = { TextButton(onClick = onPedidoCreado) { Text("Ver mis pedidos") } },
        )
    }
}

@Composable
private fun CartRow(item: CartItem, vm: CarritoViewModel) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.padding(12.dp).fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Column(Modifier.weight(1f)) {
                Text(item.nombre, style = MaterialTheme.typography.titleSmall)
                Text("${item.precio.lps()} c/u · Subtotal ${(item.precio * item.cantidad).lps()}", style = MaterialTheme.typography.bodySmall)
            }
            TextButton(onClick = { vm.cambiarCantidad(item.productoId, item.cantidad - 1) }) { Text("−") }
            Text("${item.cantidad}")
            TextButton(
                onClick = { vm.cambiarCantidad(item.productoId, item.cantidad + 1) },
                enabled = item.cantidad < item.stock,
            ) { Text("+") }
            IconButton(onClick = { vm.quitar(item.productoId) }) {
                Icon(Icons.Default.Delete, contentDescription = "Eliminar")
            }
        }
    }
}
