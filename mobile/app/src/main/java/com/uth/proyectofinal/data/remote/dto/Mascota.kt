package com.uth.proyectofinal.data.remote.dto

data class Mascota(
    val id: Int = 0,
    val nombre: String = "",
    val especie: Ref? = null,
    val raza: String? = null,
    val fechaNacimiento: String? = null,
    val pesoKg: Double? = null,
    val fotoUrl: String? = null,
)

data class MascotaRequest(
    val nombre: String,
    val especieId: Int,
    val raza: String? = null,
    val fechaNacimiento: String? = null,
    val pesoKg: Double? = null,
)

data class AplicacionVacuna(
    val id: Int = 0,
    val vacuna: Ref = Ref(),
    val fechaAplicacion: String = "",
    val proximaDosis: String? = null,
    val lote: String? = null,
    val veterinario: Ref = Ref(),
)

data class ProximaDosis(val vacuna: Ref = Ref(), val fecha: String = "")

data class Carne(
    val mascota: Ref = Ref(),
    val vacunas: List<AplicacionVacuna> = emptyList(),
    val proximasDosis: List<ProximaDosis> = emptyList(),
)

data class Historial(
    val mascota: Ref = Ref(),
    val consultas: List<Consulta> = emptyList(),
    val vacunas: List<AplicacionVacuna> = emptyList(),
    val citas: List<Cita> = emptyList(),
)

data class VacunaRequest(
    val vacunaId: Int,
    val fechaAplicacion: String,
    val lote: String? = null,
    val proximaDosis: String? = null,
    val citaId: Int? = null,
)
