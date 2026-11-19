package com.uth.proyectofinal.data.remote

import com.uth.proyectofinal.BuildConfig
import com.uth.proyectofinal.data.local.TokenStore
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.Response
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.io.IOException
import java.util.concurrent.TimeUnit

class HostFallbackInterceptor : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request()
        val originalUrl = request.url
        try {
            return chain.proceed(request)
        } catch (e: IOException) {
            val fallbackHost = when (originalUrl.host) {
                "10.0.2.2" -> "192.168.100.15"
                "192.168.100.15" -> "10.0.2.2"
                "localhost", "127.0.0.1" -> "10.0.2.2"
                else -> null
            }
            if (fallbackHost != null && fallbackHost != originalUrl.host) {
                val newUrl = originalUrl.newBuilder().host(fallbackHost).build()
                val newRequest = request.newBuilder().url(newUrl).build()
                return chain.proceed(newRequest)
            }
            throw e
        }
    }
}

class ApiClient(baseUrl: String, tokens: TokenStore, onSessionExpired: () -> Unit) {

    private val logging = HttpLoggingInterceptor().apply {
        level = if (BuildConfig.DEBUG) HttpLoggingInterceptor.Level.BASIC else HttpLoggingInterceptor.Level.NONE
        redactHeader("Authorization")
    }

    private val refreshService = retrofit(
        baseUrl,
        OkHttpClient.Builder()
            .addInterceptor(HostFallbackInterceptor())
            .addInterceptor(logging)
            .build()
    ).create(ApiService::class.java)

    private val client = OkHttpClient.Builder()
        .addInterceptor(HostFallbackInterceptor())
        .addInterceptor(AuthInterceptor(tokens))
        .addInterceptor(logging)
        .authenticator(TokenAuthenticator(tokens, refreshService, onSessionExpired))
        .connectTimeout(15, TimeUnit.SECONDS)
        .readTimeout(15, TimeUnit.SECONDS)
        .build()

    val service: ApiService = retrofit(baseUrl, client).create(ApiService::class.java)

    private fun retrofit(baseUrl: String, okHttp: OkHttpClient): Retrofit = Retrofit.Builder()
        .baseUrl(baseUrl)
        .client(okHttp)
        .addConverterFactory(GsonConverterFactory.create())
        .build()
}
