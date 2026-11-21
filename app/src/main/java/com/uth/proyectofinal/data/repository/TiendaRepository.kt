package com.uth.proyectofinal.data.repository

import com.uth.proyectofinal.data.remote.ApiService
import com.uth.proyectofinal.data.remote.ApiResponse
import com.google.gson.Gson
import com.google.gson.JsonParseException
import java.io.IOException
import com.uth.proyectofinal.data.remote.dto.ComprobanteInterno
import com.uth.proyectofinal.data.remote.dto.RegistrarCobroRequest
import com.uth.proyectofinal.data.remote.dto.ItemPedidoRequest
import com.uth.proyectofinal.data.remote.dto.EstadoPedidoRequest
import com.uth.proyectofinal.data.remote.dto.PedidoRequest

class TiendaRepository(private val api: ApiService) {

    suspend fun productos(categoria: String?, page: Int, limit: Int = 20) =
        apiPage { api.productos(categoria, page, limit) }

    suspend fun producto(id: Int) = apiCall { api.producto(id) }

    suspend fun crearPedido(items: List<ItemPedidoRequest>) =
        apiCall { api.crearPedido(PedidoRequest(items)) }

    suspend fun pedidos(page: Int, limit: Int = 20) = apiPage { api.pedidos(page, limit) }

    suspend fun pedido(id: Int) = apiCall { api.pedido(id) }

    suspend fun cambiarEstadoPedido(id: Int, estado: String) =
        apiCall { api.cambiarEstadoPedido(id, EstadoPedidoRequest(estado)) }

    suspend fun cobrarPedido(id: Int, request: RegistrarCobroRequest) = apiCall { api.cobrarPedido(id, request) }

    suspend fun cobroPedido(id: Int): ApiResult<ComprobanteInterno?> = try {
        val response = api.cobroPedido(id)
        val body = response.body()
        if (response.isSuccessful && body?.success == true && body.data != null) {
            ApiResult.Success(body.data)
        } else if (response.code() == 404) {
            val error = response.errorBody()?.string()?.let { Gson().fromJson(it, ApiResponse::class.java)?.error }
            if (error?.code == "NOT_FOUND" && error.message == "Cobro no encontrado") ApiResult.Success(null)
            else ApiResult.Error("No se pudo consultar el cobro. Verifica que la API de cobros esté disponible.")
        } else {
            ApiResult.Error("No se pudo consultar el cobro. Intenta de nuevo.")
        }
    } catch (e: IOException) {
        ApiResult.Error("Sin conexión con el servidor. Revisa tu internet.")
    } catch (e: JsonParseException) {
        ApiResult.Error("La respuesta del servidor no es válida.")
    }
}
