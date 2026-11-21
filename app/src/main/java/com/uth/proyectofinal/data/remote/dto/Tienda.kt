package com.uth.proyectofinal.data.remote.dto

data class Producto(
    val id: Int = 0,
    val nombre: String = "",
    val descripcion: String? = null,
    val categoria: String = "",
    val precio: Double = 0.0,
    val stock: Int = 0,
    val imagenUrl: String? = null,
    val activo: Boolean = true,
)

data class ProductoRequest(
    val nombre: String,
    val descripcion: String? = null,
    val categoria: String,
    val precio: Double,
    val stock: Int,
)

data class ItemPedido(
    val productoId: Int = 0,
    val nombre: String = "",
    val precioUnitario: Double = 0.0,
    val cantidad: Int = 0,
    val subtotal: Double = 0.0,
)

data class Pedido(
    val id: Int = 0,
    val fecha: String = "",
    val estado: String = "",
    val total: Double = 0.0,
    val items: List<ItemPedido> = emptyList(),
    val cliente: Ref? = null,
)

data class ItemPedidoRequest(val productoId: Int, val cantidad: Int)

data class PedidoRequest(val items: List<ItemPedidoRequest>)

data class EstadoPedidoRequest(val estado: String)

data class RegistrarCobroRequest(val metodo: String, val referenciaTransferencia: String?, val claveIdempotencia: String)

data class ComprobanteItem(val descripcion: String = "", val cantidad: Int = 0, val precioUnitario: Double = 0.0, val subtotal: Double = 0.0)

data class ComprobanteInterno(
    val id: Int = 0,
    val numeroInterno: String = "",
    val pedidoId: Int? = null,
    val fecha: String = "",
    val cliente: Ref = Ref(),
    val administrador: Ref = Ref(),
    val metodo: String = "",
    val referenciaTransferencia: String? = null,
    val importe: Double = 0.0,
    val items: List<ComprobanteItem> = emptyList(),
)

data class Notificacion(
    val id: Int = 0,
    val titulo: String = "",
    val mensaje: String = "",
    val leida: Boolean = false,
    val fecha: String = "",
)
