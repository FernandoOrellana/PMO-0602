package com.uth.proyectofinal.ui.common

import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Locale

private val FECHA_REGEX = Regex("""\d{4}-\d{2}-\d{2}""")
private val HORA_REGEX = Regex("""\d{2}:\d{2}""")

fun Double.lps(): String = "Lps. ${String.format(Locale.US, "%.2f", this)}"

fun String.esFecha(): Boolean {
    if (!FECHA_REGEX.matches(this)) return false
    val mes = substring(5, 7).toInt()
    val dia = substring(8, 10).toInt()
    return mes in 1..12 && dia in 1..31
}

fun String.esHora(): Boolean {
    if (!HORA_REGEX.matches(this)) return false
    return substring(0, 2).toInt() < 24 && substring(3, 5).toInt() < 60
}

fun String.opcional(): String? = trim().ifEmpty { null }

fun isoFecha(cal: Calendar = Calendar.getInstance()): String =
    SimpleDateFormat("yyyy-MM-dd", Locale.US).format(cal.time)

fun primerDiaDelMes(): String =
    isoFecha(Calendar.getInstance().apply { set(Calendar.DAY_OF_MONTH, 1) })

fun String?.textoFecha(): String = this?.replace('T', ' ')?.take(16).orEmpty()
