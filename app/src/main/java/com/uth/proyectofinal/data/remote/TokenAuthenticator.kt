package com.uth.proyectofinal.data.remote

import com.uth.proyectofinal.data.local.TokenStore
import com.uth.proyectofinal.data.remote.dto.RefreshRequest
import okhttp3.Authenticator
import okhttp3.Request
import okhttp3.Response
import okhttp3.Route
import java.io.IOException

class TokenAuthenticator(
    private val tokens: TokenStore,
    private val refreshService: ApiService,
    private val onSessionExpired: () -> Unit,
) : Authenticator {

    override fun authenticate(route: Route?, response: Response): Request? {
        val path = response.request.url.encodedPath
        if (path.startsWith("/api/auth/") && !path.endsWith("/me")) return null
        if (responseCount(response) >= 2) return null

        synchronized(this) {
            val sent = response.request.header("Authorization")
            val current = tokens.accessToken
            if (current != null && sent != "Bearer $current") {
                return response.request.withToken(current)
            }

            val refresh = tokens.refreshToken ?: return expire()
            val result = try {
                refreshService.refresh(RefreshRequest(refresh)).execute()
            } catch (e: IOException) {
                return null
            }
            val data = result.body()?.data
            if (data == null) {
                return if (result.code() in 400..499) expire() else null
            }
            tokens.saveTokens(data.accessToken, data.refreshToken)
            return response.request.withToken(data.accessToken)
        }
    }

    private fun expire(): Request? {
        tokens.clear()
        onSessionExpired()
        return null
    }

    private fun responseCount(response: Response): Int {
        var count = 1
        var prior = response.priorResponse
        while (prior != null) {
            count++
            prior = prior.priorResponse
        }
        return count
    }

    private fun Request.withToken(token: String): Request =
        newBuilder().header("Authorization", "Bearer $token").build()
}
