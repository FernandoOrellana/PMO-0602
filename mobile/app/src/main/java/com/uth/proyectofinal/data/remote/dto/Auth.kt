package com.uth.proyectofinal.data.remote.dto

data class Usuario(
    val id: Int = 0,
    val nombre: String = "",
    val email: String = "",
    val telefono: String? = null,
    val rol: String = "",
)

data class LoginResponse(
    val accessToken: String = "",
    val refreshToken: String = "",
    val expiresIn: Int? = null,
    val usuario: Usuario = Usuario(),
)

data class TokensDto(
    val accessToken: String = "",
    val refreshToken: String = "",
    val expiresIn: Int? = null,
)

data class RegisterRequest(
    val nombre: String,
    val email: String,
    val password: String,
    val telefono: String? = null,
)

data class VerifyEmailRequest(val email: String, val codigo: String)

data class EmailRequest(val email: String)

data class LoginRequest(val email: String, val password: String)

data class RefreshRequest(val refreshToken: String)

data class ResetPasswordRequest(
    val email: String,
    val claveTemporal: String,
    val nuevaPassword: String,
)

data class DeviceRequest(val fcmToken: String)
