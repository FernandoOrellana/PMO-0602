package com.uth.proyectofinal.data.repository

import com.uth.proyectofinal.data.remote.ApiService
import com.uth.proyectofinal.data.remote.dto.CitaRequest
import com.uth.proyectofinal.data.remote.dto.ConsultaRequest

class CitaRepository(private val api: ApiService) {

    suspend fun list(fecha: String?, estado: String?, page: Int, limit: Int = 20) =
        apiPage { api.citas(fecha, estado, page, limit) }

    suspend fun get(id: Int) = apiCall { api.cita(id) }

    suspend fun crear(body: CitaRequest) = apiCall { api.crearCita(body) }

    suspend fun cancelar(id: Int) = apiCall { api.cancelarCita(id) }

    suspend fun registrarConsulta(id: Int, body: ConsultaRequest) =
        apiCall { api.registrarConsulta(id, body) }

    suspend fun agregarObservacion(id: Int, observaciones: String) =
        apiCall { api.agregarObservacion(id, com.uth.proyectofinal.data.remote.dto.ObservacionesRequest(observaciones)) }
}
