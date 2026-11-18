package com.uth.proyectofinal.data.repository

import com.uth.proyectofinal.data.remote.ApiService
import com.uth.proyectofinal.data.remote.dto.CrearVeterinarioRequest
import com.uth.proyectofinal.data.remote.dto.EditarVeterinarioRequest
import com.uth.proyectofinal.data.remote.dto.Horario
import com.uth.proyectofinal.data.remote.dto.ProductoRequest
import com.uth.proyectofinal.data.remote.dto.ServicioRequest
import okhttp3.MultipartBody

class AdminRepository(private val api: ApiService) {

    suspend fun crearProducto(body: ProductoRequest) = apiCall { api.crearProducto(body) }

    suspend fun editarProducto(id: Int, body: ProductoRequest) =
        apiCall { api.editarProducto(id, body) }

    suspend fun eliminarProducto(id: Int) = apiCall { api.eliminarProducto(id) }.map { }

    suspend fun subirImagenProducto(id: Int, imagen: MultipartBody.Part) =
        apiCall { api.subirImagenProducto(id, imagen) }

    suspend fun crearServicio(body: ServicioRequest) = apiCall { api.crearServicio(body) }

    suspend fun editarServicio(id: Int, body: ServicioRequest) =
        apiCall { api.editarServicio(id, body) }

    suspend fun eliminarServicio(id: Int) = apiCall { api.eliminarServicio(id) }.map { }

    suspend fun crearVeterinario(body: CrearVeterinarioRequest) =
        apiCall { api.crearVeterinario(body) }

    suspend fun editarVeterinario(id: Int, body: EditarVeterinarioRequest) =
        apiCall { api.editarVeterinario(id, body) }

    suspend fun guardarHorarios(id: Int, horarios: List<Horario>) =
        apiCall { api.guardarHorarios(id, horarios) }

    suspend fun reporteVentas(desde: String, hasta: String) =
        apiCall { api.reporteVentas(desde, hasta) }

    suspend fun reporteCitas(desde: String, hasta: String) =
        apiCall { api.reporteCitas(desde, hasta) }
}
