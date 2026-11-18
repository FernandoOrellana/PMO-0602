package com.uth.proyectofinal.data.repository

import com.uth.proyectofinal.data.remote.ApiService
import com.uth.proyectofinal.data.remote.dto.ItemPedidoRequest
import com.uth.proyectofinal.data.remote.dto.PedidoRequest

class TiendaRepository(private val api: ApiService) {

    suspend fun productos(categoria: String?, page: Int, limit: Int = 20) =
        apiPage { api.productos(categoria, page, limit) }

    suspend fun producto(id: Int) = apiCall { api.producto(id) }

    suspend fun crearPedido(items: List<ItemPedidoRequest>) =
        apiCall { api.crearPedido(PedidoRequest(items)) }

    suspend fun pedidos(page: Int, limit: Int = 20) = apiPage { api.pedidos(page, limit) }

    suspend fun pedido(id: Int) = apiCall { api.pedido(id) }
}
