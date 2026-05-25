using UnityEngine;
using System.IO.Ports;
using System;

public class DadoController : MonoBehaviour
{
    [Header("Configuración del Puerto")]
    public string puertoCOM = "COM11"; // Asegúrate de que sea tu puerto
    private SerialPort puerto;

    [Header("Acelerómetro")]
    public float sensibilidadAgitacion = 25000f;

    private bool[] estadoAnterior = new bool[4];

    void Start()
    {
        puerto = new SerialPort(puertoCOM, 9600);
        puerto.ReadTimeout = 20;
        try { puerto.Open(); }
        catch (Exception e) { Debug.LogError("No se pudo abrir el puerto: " + e.Message); }
    }

    void Update()
    {
        if (puerto != null && puerto.IsOpen && puerto.BytesToRead > 0)
        {
            try
            {
                string datos = puerto.ReadLine();
                string[] partes = datos.Split('|');
                if (partes.Length < 2) return;

                string[] botones = partes[0].Split(',');
                string[] aceleracion = partes[1].Split(',');

                // Leer los 4 botones
                for (int i = 0; i < 4; i++)
                {
                    bool presionadoAhora = (botones[i] == "1");

                    if (presionadoAhora && !estadoAnterior[i])
                    {
                        Debug.Log("Clic físico en botón índice: " + i);

                        // ===== AUTO-CONEXIÓN MÁGICA =====
                        // Índice 0 = Botón Rojo (Z)
                        if (i == 0)
                        {
                            if (JuegoTablero.Instancia != null) JuegoTablero.Instancia.PresionarFisicoRojo();
                            if (MenuPrincipal.Instancia != null) MenuPrincipal.Instancia.AccionEmpezar();
                        }
                        // Índice 1 = Botón Azul (X)
                        else if (i == 1)
                        {
                            if (JuegoTablero.Instancia != null) JuegoTablero.Instancia.PresionarFisicoAzul();
                            if (MenuPrincipal.Instancia != null) MenuPrincipal.Instancia.AccionAjustes();
                        }
                        // Índice 2 = Botón Verde (C)
                        else if (i == 2)
                        {
                            if (JuegoTablero.Instancia != null) JuegoTablero.Instancia.PresionarFisicoVerde();
                            if (MenuPrincipal.Instancia != null) MenuPrincipal.Instancia.AccionComoJugar();
                        }
                        // Índice 3 = Botón Blanco (V)
                        else if (i == 3)
                        {
                            if (JuegoTablero.Instancia != null) JuegoTablero.Instancia.PresionarFisicoBlanco();
                            if (MenuPrincipal.Instancia != null) MenuPrincipal.Instancia.AccionSalir();
                        }
                    }
                    estadoAnterior[i] = presionadoAhora;
                }

                // Leer agitación
                float acX = float.Parse(aceleracion[0]);
                float acY = float.Parse(aceleracion[1]);
                float acZ = float.Parse(aceleracion[2]);

                Vector3 fuerzaMovimiento = new Vector3(acX, acY, acZ);

                if (fuerzaMovimiento.magnitude > sensibilidadAgitacion)
                {
                    puerto.Write("V"); // Sonido vibración Arduino

                    if (JuegoTablero.Instancia != null) JuegoTablero.Instancia.LanzarDadoFisico();
                    if (MenuPrincipal.Instancia != null) MenuPrincipal.Instancia.EfectoAgitarHardware();
                }
            }
            catch (TimeoutException) { }
            catch (Exception) { }
        }
    }

    public void ActivarBuzzer()
    {
        if (puerto != null && puerto.IsOpen) puerto.Write("B");
    }

    void OnApplicationQuit()
    {
        if (puerto != null && puerto.IsOpen) puerto.Close();
    }
}