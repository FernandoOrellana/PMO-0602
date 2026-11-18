package com.uth.proyectofinal.data.remote.dto

data class ServicioRequest(val nombre: String, val precio: Double, val duracionMin: Int)

data class CrearVeterinarioRequest(
    val nombre: String,
    val email: String,
    val password: String,
    val telefono: String? = null,
)

data class EditarVeterinarioRequest(val nombre: String, val telefono: String? = null)

data class ReporteVentas(
    val desde: String = "",
    val hasta: String = "",
    val totalPedidos: Int = 0,
    val totalVentas: Double = 0.0,
    val productos: List<ProductoVendido> = emptyList(),
)

data class ProductoVendido(
    val productoId: Int = 0,
    val nombre: String = "",
    val cantidad: Int = 0,
    val total: Double = 0.0,
)

data class ReporteCitas(
    val desde: String = "",
    val hasta: String = "",
    val total: Int = 0,
    val porEstado: Map<String, Int> = emptyMap(),
    val porServicio: List<ServicioCantidad> = emptyList(),
)

data class ServicioCantidad(val servicioId: Int = 0, val nombre: String = "", val cantidad: Int = 0)
