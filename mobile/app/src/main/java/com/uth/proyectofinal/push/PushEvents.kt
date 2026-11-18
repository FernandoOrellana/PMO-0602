package com.uth.proyectofinal.push

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

object PushEvents {
    private val abrirNotificaciones = MutableStateFlow(false)
    val pendiente: StateFlow<Boolean> = abrirNotificaciones.asStateFlow()

    fun solicitarAbrir() {
        abrirNotificaciones.value = true
    }

    fun consumir() {
        abrirNotificaciones.value = false
    }
}
