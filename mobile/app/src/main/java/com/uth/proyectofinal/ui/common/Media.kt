package com.uth.proyectofinal.ui.common

import android.content.Context
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.graphics.Matrix
import android.media.ExifInterface
import android.net.Uri
import androidx.core.content.FileProvider
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.toRequestBody
import java.io.ByteArrayOutputStream
import java.io.File

private const val LADO_MAXIMO = 1280
private const val CALIDAD_JPEG = 85

fun Context.autoridadArchivos(): String = "$packageName.archivos"

fun Context.nuevaFotoUri(): Uri {
    val carpeta = File(cacheDir, "fotos").apply { mkdirs() }
    val archivo = File(carpeta, "foto_${System.currentTimeMillis()}.jpg")
    return FileProvider.getUriForFile(this, autoridadArchivos(), archivo)
}

suspend fun Context.imagenPart(uri: Uri, campo: String): MultipartBody.Part = withContext(Dispatchers.IO) {
    val bytes = comprimirImagen(uri)
        ?: contentResolver.openInputStream(uri)?.use { it.readBytes() }
        ?: ByteArray(0)
    MultipartBody.Part.createFormData(campo, "imagen.jpg", bytes.toRequestBody("image/jpeg".toMediaType()))
}

private fun Context.comprimirImagen(uri: Uri): ByteArray? = runCatching {
    val limites = BitmapFactory.Options().apply { inJustDecodeBounds = true }
    contentResolver.openInputStream(uri)?.use { BitmapFactory.decodeStream(it, null, limites) }
    if (limites.outWidth <= 0 || limites.outHeight <= 0) return null

    var muestra = 1
    while (limites.outWidth / (muestra * 2) >= LADO_MAXIMO || limites.outHeight / (muestra * 2) >= LADO_MAXIMO) {
        muestra *= 2
    }
    val opciones = BitmapFactory.Options().apply { inSampleSize = muestra }
    val original = contentResolver.openInputStream(uri)?.use { BitmapFactory.decodeStream(it, null, opciones) }
        ?: return null

    val escala = minOf(1f, LADO_MAXIMO.toFloat() / maxOf(original.width, original.height))
    val matriz = Matrix().apply {
        postRotate(rotacionExif(uri))
        postScale(escala, escala)
    }
    val resultado = Bitmap.createBitmap(original, 0, 0, original.width, original.height, matriz, true)
    val salida = ByteArrayOutputStream()
    resultado.compress(Bitmap.CompressFormat.JPEG, CALIDAD_JPEG, salida)
    if (resultado !== original) resultado.recycle()
    original.recycle()
    salida.toByteArray()
}.getOrNull()

private fun Context.rotacionExif(uri: Uri): Float = runCatching {
    contentResolver.openInputStream(uri)?.use { entrada ->
        when (ExifInterface(entrada).getAttributeInt(ExifInterface.TAG_ORIENTATION, ExifInterface.ORIENTATION_NORMAL)) {
            ExifInterface.ORIENTATION_ROTATE_90 -> 90f
            ExifInterface.ORIENTATION_ROTATE_180 -> 180f
            ExifInterface.ORIENTATION_ROTATE_270 -> 270f
            else -> 0f
        }
    } ?: 0f
}.getOrDefault(0f)
