package com.uth.proyectofinal.data.remote

import com.uth.proyectofinal.BuildConfig
import com.uth.proyectofinal.data.local.TokenStore
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

class ApiClient(baseUrl: String, tokens: TokenStore, onSessionExpired: () -> Unit) {

    private val logging = HttpLoggingInterceptor().apply {
        level = if (BuildConfig.DEBUG) HttpLoggingInterceptor.Level.BASIC else HttpLoggingInterceptor.Level.NONE
        redactHeader("Authorization")
    }

    private val refreshService = retrofit(baseUrl, OkHttpClient.Builder().addInterceptor(logging).build())
        .create(ApiService::class.java)

    private val client = OkHttpClient.Builder()
        .addInterceptor(AuthInterceptor(tokens))
        .addInterceptor(logging)
        .authenticator(TokenAuthenticator(tokens, refreshService, onSessionExpired))
        .connectTimeout(30, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .build()

    val service: ApiService = retrofit(baseUrl, client).create(ApiService::class.java)

    private fun retrofit(baseUrl: String, okHttp: OkHttpClient): Retrofit = Retrofit.Builder()
        .baseUrl(baseUrl)
        .client(okHttp)
        .addConverterFactory(GsonConverterFactory.create())
        .build()
}
