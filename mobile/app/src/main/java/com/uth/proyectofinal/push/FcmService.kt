package com.uth.proyectofinal.push

import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import com.uth.proyectofinal.PetCareApp

class FcmService : FirebaseMessagingService() {

    override fun onNewToken(token: String) {
        (application as PetCareApp).container.push.send(token)
    }

    override fun onMessageReceived(message: RemoteMessage) {
        val title = message.notification?.title ?: message.data["titulo"] ?: "PetCare HN"
        val body = message.notification?.body ?: message.data["mensaje"].orEmpty()
        PushNotifications.show(this, title, body)
    }
}
