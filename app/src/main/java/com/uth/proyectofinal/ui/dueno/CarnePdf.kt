package com.uth.proyectofinal.ui.dueno

import android.content.Context
import android.content.Intent
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.Typeface
import android.graphics.pdf.PdfDocument
import androidx.core.content.FileProvider
import com.uth.proyectofinal.data.remote.dto.Carne
import com.uth.proyectofinal.data.remote.dto.Historial
import com.uth.proyectofinal.ui.common.autoridadArchivos
import com.uth.proyectofinal.ui.common.textoFecha
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.File
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

private const val ANCHO = 595
private const val ALTO = 842
private const val MARGEN = 40f
private const val PIE = 60f
private val VERDE = Color.rgb(0, 105, 92)
private val VERDE_CLARO = Color.rgb(224, 242, 238)
private val GRIS = Color.rgb(90, 90, 90)

private class Lienzo {
    val documento = PdfDocument()
    private var numero = 0
    private var pagina: PdfDocument.Page? = null
    lateinit var canvas: Canvas
    var y = MARGEN

    val titulo = Paint(Paint.ANTI_ALIAS_FLAG).apply { textSize = 13f; typeface = Typeface.DEFAULT_BOLD; color = VERDE }
    val negrita = Paint(Paint.ANTI_ALIAS_FLAG).apply { textSize = 9.5f; typeface = Typeface.DEFAULT_BOLD; color = Color.BLACK }
    val normal = Paint(Paint.ANTI_ALIAS_FLAG).apply { textSize = 9.5f; color = Color.BLACK }
    val tenue = Paint(Paint.ANTI_ALIAS_FLAG).apply { textSize = 8.5f; color = GRIS }
    val relleno = Paint().apply { style = Paint.Style.FILL }

    fun nuevaPagina() {
        cerrarPagina()
        numero++
        pagina = documento.startPage(PdfDocument.PageInfo.Builder(ANCHO, ALTO, numero).create())
        canvas = pagina!!.canvas
        y = MARGEN
    }

    fun cerrarPagina() {
        pagina?.let {
            canvas.drawText("PetCare HN · Veterinaria Huellitas · Página $numero", MARGEN, ALTO - 25f, tenue)
            documento.finishPage(it)
        }
        pagina = null
    }

    fun espacio(alto: Float) {
        if (y + alto > ALTO - PIE) nuevaPagina()
    }

    fun recortar(texto: String, ancho: Float, paint: Paint): String {
        if (paint.measureText(texto) <= ancho) return texto
        var fin = texto.length
        while (fin > 0 && paint.measureText(texto.substring(0, fin) + "…") > ancho) fin--
        return texto.substring(0, fin) + "…"
    }
}

suspend fun Context.generarCarnePdf(nombreMascota: String, carne: Carne?, historial: Historial?): File =
    withContext(Dispatchers.IO) {
        val l = Lienzo()
        l.nuevaPagina()
        encabezado(l, nombreMascota)
        tablaVacunas(l, carne)
        proximasDosis(l, carne)
        consultas(l, historial)
        l.cerrarPagina()

        val carpeta = File(cacheDir, "carnes").apply { mkdirs() }
        val nombre = nombreMascota.replace(Regex("[^A-Za-z0-9]+"), "_").trim('_').ifEmpty { "mascota" }
        val archivo = File(carpeta, "carne_$nombre.pdf")
        archivo.outputStream().use { l.documento.writeTo(it) }
        l.documento.close()
        archivo
    }

fun Context.compartirPdf(archivo: File) {
    val uri = FileProvider.getUriForFile(this, autoridadArchivos(), archivo)
    val enviar = Intent(Intent.ACTION_SEND).apply {
        type = "application/pdf"
        putExtra(Intent.EXTRA_STREAM, uri)
        putExtra(Intent.EXTRA_SUBJECT, "Carné de vacunación")
        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
    }
    startActivity(Intent.createChooser(enviar, "Compartir carné").addFlags(Intent.FLAG_ACTIVITY_NEW_TASK))
}

private fun encabezado(l: Lienzo, nombreMascota: String) {
    l.relleno.color = VERDE
    l.canvas.drawRect(0f, 0f, ANCHO.toFloat(), 96f, l.relleno)
    val blanco = Paint(Paint.ANTI_ALIAS_FLAG).apply { color = Color.WHITE }
    blanco.textSize = 20f
    blanco.typeface = Typeface.DEFAULT_BOLD
    l.canvas.drawText("Carné de vacunación digital", MARGEN, 42f, blanco)
    blanco.textSize = 11f
    blanco.typeface = Typeface.DEFAULT
    l.canvas.drawText("Veterinaria Huellitas · Comayagüela · PetCare HN", MARGEN, 62f, blanco)
    val emitido = SimpleDateFormat("dd/MM/yyyy HH:mm", Locale.getDefault()).format(Date())
    l.canvas.drawText("Emitido el $emitido", MARGEN, 80f, blanco)

    l.y = 124f
    l.relleno.color = VERDE_CLARO
    l.canvas.drawRect(MARGEN, l.y - 16f, ANCHO - MARGEN, l.y + 12f, l.relleno)
    l.canvas.drawText("Mascota: $nombreMascota", MARGEN + 10f, l.y + 2f, l.titulo)
    l.y += 36f
}

private fun seccion(l: Lienzo, texto: String) {
    l.espacio(40f)
    l.canvas.drawText(texto, MARGEN, l.y, l.titulo)
    l.y += 8f
    l.relleno.color = VERDE
    l.canvas.drawRect(MARGEN, l.y, ANCHO - MARGEN, l.y + 1.5f, l.relleno)
    l.y += 16f
}

private fun fila(l: Lienzo, columnas: List<Float>, textos: List<String>, paint: Paint, fondo: Int? = null) {
    l.espacio(20f)
    if (fondo != null) {
        l.relleno.color = fondo
        l.canvas.drawRect(MARGEN, l.y - 12f, ANCHO - MARGEN, l.y + 5f, l.relleno)
    }
    var x = MARGEN + 4f
    textos.forEachIndexed { i, t ->
        val ancho = columnas[i] - 8f
        l.canvas.drawText(l.recortar(t, ancho, paint), x, l.y, paint)
        x += columnas[i]
    }
    l.y += 18f
}

private fun tablaVacunas(l: Lienzo, carne: Carne?) {
    seccion(l, "Vacunas aplicadas")
    val vacunas = carne?.vacunas.orEmpty()
    if (vacunas.isEmpty()) {
        l.canvas.drawText("Aún no tiene vacunas registradas.", MARGEN, l.y, l.normal)
        l.y += 24f
        return
    }
    val columnas = listOf(130f, 80f, 85f, 130f, 90f)
    fila(l, columnas, listOf("Vacuna", "Aplicada", "Próxima dosis", "Veterinario", "Lote"), l.negrita, VERDE_CLARO)
    vacunas.forEachIndexed { i, v ->
        fila(
            l,
            columnas,
            listOf(v.vacuna.nombre, v.fechaAplicacion, v.proximaDosis ?: "—", v.veterinario.nombre, v.lote ?: "—"),
            l.normal,
            if (i % 2 == 1) Color.rgb(247, 247, 247) else null,
        )
    }
    l.y += 10f
}

private fun proximasDosis(l: Lienzo, carne: Carne?) {
    seccion(l, "Próximas dosis")
    val dosis = carne?.proximasDosis.orEmpty()
    if (dosis.isEmpty()) {
        l.canvas.drawText("No hay dosis pendientes.", MARGEN, l.y, l.normal)
        l.y += 24f
        return
    }
    dosis.forEach { d ->
        val estado = estadoDosis(d.fecha)
        fila(l, listOf(230f, 110f, 175f), listOf(d.vacuna.nombre, d.fecha, estado.texto), l.normal)
    }
    l.y += 10f
}

private fun consultas(l: Lienzo, historial: Historial?) {
    seccion(l, "Historial clínico")
    val lista = historial?.consultas.orEmpty()
    if (lista.isEmpty()) {
        l.canvas.drawText("Sin consultas registradas.", MARGEN, l.y, l.normal)
        l.y += 24f
        return
    }
    lista.forEach { c ->
        l.espacio(52f)
        l.canvas.drawText("${c.fecha.textoFecha()} · ${c.veterinario.nombre}", MARGEN, l.y, l.tenue)
        l.y += 14f
        l.canvas.drawText(l.recortar("Diagnóstico: ${c.diagnostico}", ANCHO - 2 * MARGEN, l.negrita), MARGEN, l.y, l.negrita)
        l.y += 14f
        l.canvas.drawText(l.recortar("Tratamiento: ${c.tratamiento ?: "—"}", ANCHO - 2 * MARGEN, l.normal), MARGEN, l.y, l.normal)
        l.y += 20f
    }
}
