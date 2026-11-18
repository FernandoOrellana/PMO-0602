package com.uth.proyectofinal.data.remote

import com.uth.proyectofinal.data.local.TokenStore
import okhttp3.Interceptor
import okhttp3.Response

class AuthInterceptor(private val tokens: TokenStore) : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request()
        val token = tokens.accessToken
        if (token.isNullOrBlank() || request.header("Authorization") != null) {
            return chain.proceed(request)
        }
        return chain.proceed(request.newBuilder().header("Authorization", "Bearer $token").build())
    }
}
