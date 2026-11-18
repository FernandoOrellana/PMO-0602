package com.uth.proyectofinal.data.local

import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asSharedFlow

object SessionEvents {
    private val expired = MutableSharedFlow<Unit>(extraBufferCapacity = 1)
    val logout: SharedFlow<Unit> = expired.asSharedFlow()

    fun emitLogout() {
        expired.tryEmit(Unit)
    }
}
