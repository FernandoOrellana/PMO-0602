package com.uth.proyectofinal.ui.common

import android.content.Context
import android.net.Uri
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.toRequestBody

suspend fun Context.imagenPart(uri: Uri, campo: String): MultipartBody.Part = withContext(Dispatchers.IO) {
    val tipo = contentResolver.getType(uri) ?: "image/jpeg"
    val bytes = contentResolver.openInputStream(uri)?.use { it.readBytes() } ?: ByteArray(0)
    MultipartBody.Part.createFormData(campo, "imagen", bytes.toRequestBody(tipo.toMediaType()))
}
