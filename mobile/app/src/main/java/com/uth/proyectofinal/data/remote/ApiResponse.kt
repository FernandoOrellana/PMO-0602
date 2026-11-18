package com.uth.proyectofinal.data.remote

import com.google.gson.JsonElement

data class ApiResponse<T>(
    val success: Boolean = false,
    val data: T? = null,
    val meta: PageMeta? = null,
    val error: ApiError? = null,
)

data class PageMeta(
    val page: Int = 1,
    val limit: Int = 20,
    val total: Int = 0,
    val totalPages: Int = 1,
)

data class ApiError(
    val code: String? = null,
    val message: String? = null,
    val details: JsonElement? = null,
)
