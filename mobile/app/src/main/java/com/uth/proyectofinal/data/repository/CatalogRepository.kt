package com.uth.proyectofinal.data.repository

import com.uth.proyectofinal.data.remote.ApiService

class CatalogRepository(private val api: ApiService) {

    suspend fun especies() = apiCall { api.especies() }

    suspend fun vacunas() = apiCall { api.vacunas() }

    suspend fun servicios() = apiCall { api.servicios() }

    suspend fun veterinarios() = apiCall { api.veterinarios() }

    suspend fun disponibilidad(veterinarioId: Int, fecha: String) =
        apiCall { api.disponibilidad(veterinarioId, fecha) }.map { it.horas }
}
