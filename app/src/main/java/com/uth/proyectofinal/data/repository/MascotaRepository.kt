package com.uth.proyectofinal.data.repository

import com.uth.proyectofinal.data.remote.ApiService
import com.uth.proyectofinal.data.remote.dto.MascotaRequest
import com.uth.proyectofinal.data.remote.dto.VacunaRequest
import okhttp3.MultipartBody

class MascotaRepository(private val api: ApiService) {

    suspend fun list(page: Int, limit: Int = 20) = apiPage { api.mascotas(page, limit) }

    suspend fun get(id: Int) = apiCall { api.mascota(id) }

    suspend fun create(body: MascotaRequest) = apiCall { api.crearMascota(body) }

    suspend fun update(id: Int, body: MascotaRequest) = apiCall { api.editarMascota(id, body) }

    suspend fun delete(id: Int) = apiCall { api.eliminarMascota(id) }.map { }

    suspend fun subirFoto(id: Int, foto: MultipartBody.Part) =
        apiCall { api.subirFotoMascota(id, foto) }

    suspend fun carne(id: Int) = apiCall { api.carne(id) }

    suspend fun historial(id: Int) = apiCall { api.historial(id) }

    suspend fun citas(id: Int) = apiCall { api.citasMascota(id) }

    suspend fun registrarVacuna(id: Int, body: VacunaRequest) =
        apiCall { api.registrarVacuna(id, body) }
}
