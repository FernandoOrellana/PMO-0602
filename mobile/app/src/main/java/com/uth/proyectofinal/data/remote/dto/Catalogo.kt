package com.uth.proyectofinal.data.remote.dto

data class Especie(val id: Int = 0, val nombre: String = "")

data class Vacuna(val id: Int = 0, val nombre: String = "", val intervaloDias: Int? = null)

data class Servicio(
    val id: Int = 0,
    val nombre: String = "",
    val precio: Double = 0.0,
    val duracionMin: Int = 0,
    val activo: Boolean = true,
)

data class Horario(val diaSemana: Int, val horaInicio: String, val horaFin: String)

data class Veterinario(
    val id: Int = 0,
    val nombre: String = "",
    val email: String? = null,
    val telefono: String? = null,
    val horarios: List<Horario> = emptyList(),
)

data class Disponibilidad(
    val fecha: String = "",
    val veterinario: Ref = Ref(),
    val horas: List<String> = emptyList(),
)
