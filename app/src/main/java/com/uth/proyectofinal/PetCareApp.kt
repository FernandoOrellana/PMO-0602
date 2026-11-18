package com.uth.proyectofinal

import android.app.Application
import com.uth.proyectofinal.push.PushNotifications

class PetCareApp : Application() {

    lateinit var container: AppContainer
        private set

    override fun onCreate() {
        super.onCreate()
        container = AppContainer(this)
        PushNotifications.createChannel(this)
    }
}
