package com.uth.proyectofinal.ui.admin

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Card
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.compose.viewModel
import com.uth.proyectofinal.data.remote.dto.ReporteCitas
import com.uth.proyectofinal.data.remote.dto.ReporteVentas
import com.uth.proyectofinal.data.repository.AdminRepository
import com.uth.proyectofinal.data.repository.ApiResult
import com.uth.proyectofinal.ui.common.ActionButton
import com.uth.proyectofinal.ui.common.AppScaffold
import com.uth.proyectofinal.ui.common.AppTextField
import com.uth.proyectofinal.ui.common.ErrorText
import com.uth.proyectofinal.ui.common.SectionTitle
import com.uth.proyectofinal.ui.common.appFactory
import com.uth.proyectofinal.ui.common.esFecha
import com.uth.proyectofinal.ui.common.isoFecha
import com.uth.proyectofinal.ui.common.lps
import com.uth.proyectofinal.ui.common.primerDiaDelMes
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class ReportesUi(
    val desde: String = primerDiaDelMes(),
    val hasta: String = isoFecha(),
    val ventas: ReporteVentas? = null,
    val citas: ReporteCitas? = null,
    val cargando: Boolean = false,
    val error: String? = null,
)

class ReportesViewModel(private val admin: AdminRepository) : ViewModel() {

    private val _ui = MutableStateFlow(ReportesUi())
    val ui: StateFlow<ReportesUi> = _ui.asStateFlow()

    fun onDesde(value: String) = _ui.update { it.copy(desde = value, error = null) }
    fun onHasta(value: String) = _ui.update { it.copy(hasta = value, error = null) }

    fun generar() {
        val s = _ui.value
        if (!s.desde.esFecha() || !s.hasta.esFecha()) {
            _ui.update { it.copy(error = "Las fechas deben tener formato AAAA-MM-DD.") }
            return
        }
        viewModelScope.launch {
            _ui.update { it.copy(cargando = true, error = null) }
            val ventas = admin.reporteVentas(s.desde, s.hasta)
            val citas = admin.reporteCitas(s.desde, s.hasta)
            val error = (ventas as? ApiResult.Error)?.message ?: (citas as? ApiResult.Error)?.message
            _ui.update {
                it.copy(
                    cargando = false,
                    ventas = (ventas as? ApiResult.Success)?.data,
                    citas = (citas as? ApiResult.Success)?.data,
                    error = error,
                )
            }
        }
    }
}

@Composable
fun ReportesScreen() {
    val vm: ReportesViewModel = viewModel(factory = appFactory { ReportesViewModel(it.admin) })
    val ui by vm.ui.collectAsStateWithLifecycle()

    AppScaffold(title = "Reportes") { padding ->
        Column(
            modifier = Modifier
                .padding(padding)
                .padding(16.dp)
                .fillMaxSize()
                .verticalScroll(rememberScrollState()),
            verticalArrangement = Arrangement.spacedBy(10.dp),
        ) {
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
                AppTextField(ui.desde, vm::onDesde, "Desde (AAAA-MM-DD)", modifier = Modifier.weight(1f))
                AppTextField(ui.hasta, vm::onHasta, "Hasta (AAAA-MM-DD)", modifier = Modifier.weight(1f))
            }
            ErrorText(ui.error)
            ActionButton("Generar reportes", ui.cargando, vm::generar)

            ui.ventas?.let { v ->
                SectionTitle("Ventas")
                Card(modifier = Modifier.fillMaxWidth()) {
                    Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                        Text("Total ventas: ${v.totalVentas.lps()}", style = MaterialTheme.typography.titleMedium)
                        Text("Pedidos: ${v.totalPedidos}")
                        Text("Productos más vendidos:", style = MaterialTheme.typography.titleSmall)
                        if (v.productos.isEmpty()) Text("Sin ventas en el periodo.")
                        v.productos.forEach { p ->
                            Text("• ${p.nombre}: ${p.cantidad} uds. (${p.total.lps()})")
                        }
                    }
                }
            }

            ui.citas?.let { c ->
                SectionTitle("Citas")
                Card(modifier = Modifier.fillMaxWidth()) {
                    Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                        Text("Total citas: ${c.total}", style = MaterialTheme.typography.titleMedium)
                        c.porEstado.forEach { (estado, cantidad) -> Text("$estado: $cantidad") }
                        Text("Por servicio:", style = MaterialTheme.typography.titleSmall)
                        if (c.porServicio.isEmpty()) Text("Sin citas en el periodo.")
                        c.porServicio.forEach { s -> Text("• ${s.nombre}: ${s.cantidad}") }
                    }
                }
            }
        }
    }
}
