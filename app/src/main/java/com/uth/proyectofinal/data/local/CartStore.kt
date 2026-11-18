package com.uth.proyectofinal.data.local

import com.uth.proyectofinal.data.remote.dto.Producto
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update

data class CartItem(
    val productoId: Int,
    val nombre: String,
    val precio: Double,
    val stock: Int,
    val imagenUrl: String?,
    val cantidad: Int,
)

class CartStore {
    private val _items = MutableStateFlow<List<CartItem>>(emptyList())
    val items: StateFlow<List<CartItem>> = _items.asStateFlow()

    fun add(producto: Producto): Boolean {
        if (producto.stock <= 0) return false
        _items.update { list ->
            val existing = list.find { it.productoId == producto.id }
            when {
                existing == null -> list + CartItem(
                    producto.id, producto.nombre, producto.precio, producto.stock, producto.imagenUrl, 1,
                )
                existing.cantidad < existing.stock -> list.map {
                    if (it.productoId == producto.id) it.copy(cantidad = it.cantidad + 1) else it
                }
                else -> list
            }
        }
        return true
    }

    fun setCantidad(productoId: Int, cantidad: Int) {
        _items.update { list ->
            if (cantidad <= 0) {
                list.filterNot { it.productoId == productoId }
            } else {
                list.map {
                    if (it.productoId == productoId) it.copy(cantidad = cantidad.coerceAtMost(it.stock)) else it
                }
            }
        }
    }

    fun clear() {
        _items.value = emptyList()
    }
}
