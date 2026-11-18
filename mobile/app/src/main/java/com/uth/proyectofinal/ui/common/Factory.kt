package com.uth.proyectofinal.ui.common

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.uth.proyectofinal.AppContainer
import com.uth.proyectofinal.PetCareApp

inline fun <reified VM : ViewModel> appFactory(noinline create: (AppContainer) -> VM): ViewModelProvider.Factory =
    viewModelFactory {
        initializer {
            val app = this[ViewModelProvider.AndroidViewModelFactory.APPLICATION_KEY] as PetCareApp
            create(app.container)
        }
    }
