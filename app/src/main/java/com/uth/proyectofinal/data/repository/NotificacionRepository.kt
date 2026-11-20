package com.uth.proyectofinal.data.repository

import com.uth.proyectofinal.data.remote.ApiService
import com.uth.proyectofinal.data.remote.dto.DeviceRequest

class NotificacionRepository(private val api: ApiService) {

    suspend fun list() = apiCall { api.notificaciones() }

    suspend fun marcarLeida(id: Int) = apiCall { api.leerNotificacion(id) }

    suspend fun probar() = apiCall { api.probarNotificacion() }

    suspend fun registrarDispositivo(fcmToken: String) =
        apiCall { api.registrarDispositivo(DeviceRequest(fcmToken)) }.map { }
}
