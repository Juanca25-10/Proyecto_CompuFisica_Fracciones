using UnityEngine;
using System.IO.Ports;
using System;
using UnityEngine.Events;

public class DadoController : MonoBehaviour
{
    [Header("Configuración del Puerto")]
    // Cambia "COM3" por el puerto real de tu Arduino en Windows
    public string puertoCOM = "COM11";
    private SerialPort puerto;

    [Header("Acelerómetro")]
    public float sensibilidadAgitacion = 25000f;

    [Header("Eventos de los 4 Botones")]
    public UnityEvent[] alPresionarBoton = new UnityEvent[4];
    private bool[] estadoAnterior = new bool[4];

    void Start()
    {
        puerto = new SerialPort(puertoCOM, 9600);
        puerto.ReadTimeout = 20;
        try
        {
            puerto.Open();
        }
        catch (Exception e)
        {
            Debug.LogError("No se pudo abrir el puerto: " + e.Message);
        }
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

                // Leer botones
                for (int i = 0; i < 4; i++)
                {
                    bool presionadoAhora = (botones[i] == "1");

                    if (presionadoAhora && !estadoAnterior[i])
                    {
                        Debug.Log("Clic físico en botón índice: " + i);
                        alPresionarBoton[i].Invoke();
                    }
                    estadoAnterior[i] = presionadoAhora;
                }

                // Leer agitación del MPU
                float acX = float.Parse(aceleracion[0]);
                float acY = float.Parse(aceleracion[1]);
                float acZ = float.Parse(aceleracion[2]);

                Vector3 fuerzaMovimiento = new Vector3(acX, acY, acZ);

                if (fuerzaMovimiento.magnitude > sensibilidadAgitacion)
                {
                    // Enviar sonido de vibración al Arduino
                    puerto.Write("V");

                    // Ordenar al tablero lanzar el dado
                    JuegoTablero juego = FindObjectOfType<JuegoTablero>();
                    if (juego != null) juego.LanzarDadoFisico();
                }
            }
            catch (TimeoutException) { }
            catch (Exception) { }
        }
    }

    public void ActivarBuzzer()
    {
        if (puerto != null && puerto.IsOpen)
        {
            puerto.Write("B");
        }
    }

    void OnApplicationQuit()
    {
        if (puerto != null && puerto.IsOpen) puerto.Close();
    }
}