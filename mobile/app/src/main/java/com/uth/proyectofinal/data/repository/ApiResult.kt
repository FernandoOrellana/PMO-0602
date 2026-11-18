package com.uth.proyectofinal.data.repository

import com.google.gson.Gson
import com.google.gson.JsonParseException
import com.uth.proyectofinal.data.remote.ApiError
import com.uth.proyectofinal.data.remote.ApiResponse
import com.uth.proyectofinal.data.remote.PageMeta
import retrofit2.HttpException
import java.io.IOException

sealed interface ApiResult<out T> {
    data class Success<out T>(val data: T) : ApiResult<T>
    data class Error(val message: String, val code: String? = null) : ApiResult<Nothing>
}

data class Page<T>(val data: T, val meta: PageMeta?)

inline fun <T, R> ApiResult<T>.map(transform: (T) -> R): ApiResult<R> = when (this) {
    is ApiResult.Success -> ApiResult.Success(transform(data))
    is ApiResult.Error -> this
}

fun <T> ApiResult<T>.dataOrNull(): T? = (this as? ApiResult.Success)?.data

suspend fun <T> apiCall(block: suspend () -> ApiResponse<T>): ApiResult<T> =
    apiPage(block).map { it.data }

@Suppress("UNCHECKED_CAST")
suspend fun <T> apiPage(block: suspend () -> ApiResponse<T>): ApiResult<Page<T>> = try {
    val body = block()
    if (body.success) {
        ApiResult.Success(Page(body.data as T, body.meta))
    } else {
        failure(body.error, 0)
    }
} catch (e: HttpException) {
    failure(parseError(e), e.code())
} catch (e: IOException) {
    ApiResult.Error("Sin conexión con el servidor. Revisa tu internet.")
} catch (e: JsonParseException) {
    ApiResult.Error("La respuesta del servidor no es válida.")
}

private fun parseError(e: HttpException): ApiError? = runCatching {
    e.response()?.errorBody()?.string()?.let { Gson().fromJson(it, ApiResponse::class.java)?.error }
}.getOrNull()

private fun failure(error: ApiError?, status: Int): ApiResult.Error {
    val message = when (error?.code) {
        "VALIDATION_ERROR" -> error.message ?: "Revisa los datos ingresados."
        "UNAUTHORIZED" -> "Tu sesión expiró. Inicia sesión de nuevo."
        "INVALID_CREDENTIALS" -> "Correo o contraseña incorrectos."
        "EMAIL_NOT_VERIFIED" -> "Debes verificar tu correo antes de ingresar."
        "FORBIDDEN" -> "No tienes permiso para realizar esta acción."
        "NOT_FOUND" -> "No se encontró la información solicitada."
        "CONFLICT" -> error.message ?: "Ya existe un registro con esos datos."
        "INTERNAL_ERROR" -> "Error del servidor. Intenta más tarde."
        else -> when {
            status == 401 -> "Tu sesión expiró. Inicia sesión de nuevo."
            status >= 500 -> "Error del servidor. Intenta más tarde."
            else -> error?.message ?: "No se pudo completar la operación."
        }
    }
    return ApiResult.Error(message, error?.code)
}
