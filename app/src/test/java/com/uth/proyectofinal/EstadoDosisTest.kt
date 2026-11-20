package com.uth.proyectofinal

import com.uth.proyectofinal.ui.dueno.NivelDosis
import com.uth.proyectofinal.ui.dueno.estadoDosis
import org.junit.Assert.assertEquals
import org.junit.Test
import java.util.Calendar

class EstadoDosisTest {

    private val hoy: Calendar = Calendar.getInstance().apply {
        clear()
        set(2030, Calendar.JANUARY, 10, 9, 30)
    }

    @Test
    fun dosisPasadaEstaVencida() {
        val estado = estadoDosis("2030-01-05", hoy)
        assertEquals(NivelDosis.VENCIDA, estado.nivel)
        assertEquals("Vencida hace 5 días", estado.texto)
    }

    @Test
    fun dosisDeHoyCorrespondeHoy() {
        val estado = estadoDosis("2030-01-10", hoy)
        assertEquals(NivelDosis.PROXIMA, estado.nivel)
        assertEquals("Corresponde hoy", estado.texto)
    }

    @Test
    fun dosisDentroDeSieteDiasEsProxima() {
        val estado = estadoDosis("2030-01-15", hoy)
        assertEquals(NivelDosis.PROXIMA, estado.nivel)
        assertEquals("En 5 días", estado.texto)
    }

    @Test
    fun dosisLejanaEstaProgramada() {
        assertEquals(NivelDosis.PROGRAMADA, estadoDosis("2030-03-01", hoy).nivel)
    }

    @Test
    fun fechaInvalidaNoRompeLaPantalla() {
        assertEquals(NivelDosis.DESCONOCIDA, estadoDosis("sin-fecha", hoy).nivel)
    }
}
