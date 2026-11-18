package com.uth.proyectofinal.ui.dueno

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Card
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.uth.proyectofinal.data.remote.dto.Pedido
import com.uth.proyectofinal.data.repository.ApiResult
import com.uth.proyectofinal.data.repository.Page
import com.uth.proyectofinal.data.repository.TiendaRepository
import com.uth.proyectofinal.ui.common.AppScaffold
import com.uth.proyectofinal.ui.common.EstadoBadge
import com.uth.proyectofinal.ui.common.OnResume
import com.uth.proyectofinal.ui.common.PagedColumn
import com.uth.proyectofinal.ui.common.PagedViewModel
import com.uth.proyectofinal.ui.common.appFactory
import com.uth.proyectofinal.ui.common.lps
import com.uth.proyectofinal.ui.common.textoFecha

class PedidosViewModel(private val tienda: TiendaRepository) : PagedViewModel<Pedido>() {
    override suspend fun fetch(page: Int): ApiResult<Page<List<Pedido>>> = tienda.pedidos(page)
}

@Composable
fun PedidosScreen(onOpen: (Int) -> Unit) {
    val vm: PedidosViewModel = viewModel(factory = appFactory { PedidosViewModel(it.tienda) })
    val state by vm.state.collectAsStateWithLifecycle()

    OnResume { vm.refresh() }

    AppScaffold(title = "Mis pedidos") { padding ->
        PagedColumn(
            state = state,
            emptyText = "Aún no tienes pedidos.",
            onLoadMore = vm::loadMore,
            modifier = Modifier.padding(padding),
        ) { pedido ->
            Card(onClick = { onOpen(pedido.id) }, modifier = Modifier.fillMaxWidth()) {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Text("Pedido #${pedido.id}", style = MaterialTheme.typography.titleMedium, modifier = Modifier.weight(1f))
                        EstadoBadge(pedido.estado)
                    }
                    Text(pedido.fecha.textoFecha(), style = MaterialTheme.typography.bodySmall)
                    Text("Total: ${pedido.total.lps()} · ${pedido.items.size} producto(s)")
                }
            }
        }
    }
}
