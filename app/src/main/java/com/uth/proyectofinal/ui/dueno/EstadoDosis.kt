package com.uth.proyectofinal.ui.dueno

import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Locale
import java.util.concurrent.TimeUnit

enum class NivelDosis { VENCIDA, PROXIMA, PROGRAMADA, DESCONOCIDA }

data class EstadoDosis(val nivel: NivelDosis, val texto: String)

fun estadoDosis(fecha: String, hoy: Calendar = Calendar.getInstance()): EstadoDosis {
    val objetivo = runCatching {
        SimpleDateFormat("yyyy-MM-dd", Locale.US).apply { isLenient = false }.parse(fecha.take(10))
    }.getOrNull() ?: return EstadoDosis(NivelDosis.DESCONOCIDA, "Fecha no válida")

    val inicioHoy = (hoy.clone() as Calendar).apply {
        set(Calendar.HOUR_OF_DAY, 0)
        set(Calendar.MINUTE, 0)
        set(Calendar.SECOND, 0)
        set(Calendar.MILLISECOND, 0)
    }
    val dias = Math.round((objetivo.time - inicioHoy.timeInMillis) / TimeUnit.DAYS.toMillis(1).toDouble()).toInt()
    return when {
        dias < 0 -> EstadoDosis(NivelDosis.VENCIDA, if (dias == -1) "Vencida hace 1 día" else "Vencida hace ${-dias} días")
        dias == 0 -> EstadoDosis(NivelDosis.PROXIMA, "Corresponde hoy")
        dias <= 7 -> EstadoDosis(NivelDosis.PROXIMA, if (dias == 1) "Mañana" else "En $dias días")
        else -> EstadoDosis(NivelDosis.PROGRAMADA, "En $dias días")
    }
}
