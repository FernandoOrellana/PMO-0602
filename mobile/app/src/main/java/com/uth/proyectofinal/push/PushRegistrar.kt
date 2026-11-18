package com.uth.proyectofinal.push

import com.google.firebase.messaging.FirebaseMessaging
import com.uth.proyectofinal.data.local.TokenStore
import com.uth.proyectofinal.data.repository.NotificacionRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch

class PushRegistrar(
    private val notificaciones: NotificacionRepository,
    private val tokens: TokenStore,
) {
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    fun sync() {
        if (tokens.accessToken == null) return
        runCatching {
            FirebaseMessaging.getInstance().token.addOnSuccessListener { send(it) }
        }
    }

    fun send(fcmToken: String) {
        if (tokens.accessToken == null) return
        scope.launch { notificaciones.registrarDispositivo(fcmToken) }
    }
}
