using UnityEngine;
using System.IO.Ports;
using System;
using UnityEngine.Events;

public class DadoController : MonoBehaviour
{
    SerialPort puerto = new SerialPort("COM11", 9600); // Recuerda verificar tu COM
    public float sensibilidadAgitacion = 25000f;

    [Header("Eventos de los 4 Botones")]
    // Ahora el Inspector mostrará solo 4 espacios
    public UnityEvent[] alPresionarBoton = new UnityEvent[4];
    private bool[] estadoAnterior = new bool[4];

    void Start()
    {
        puerto.ReadTimeout = 20;
        puerto.Open();
    }

    void Update()
    {
        if (puerto.IsOpen && puerto.BytesToRead > 0)
        {
            try
            {
                string datos = puerto.ReadLine();
                string[] partes = datos.Split('|');
                if (partes.Length < 2) return;

                string[] botones = partes[0].Split(',');
                string[] aceleracion = partes[1].Split(',');

                // Leer solo los 4 botones
                for (int i = 0; i < 4; i++)
                {
                    bool presionadoAhora = (botones[i] == "1");

                    if (presionadoAhora && !estadoAnterior[i])
                    {
                        Debug.Log("Clic en botón índice: " + i);
                        alPresionarBoton[i].Invoke();
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
                    Debug.Log("¡DADO AGITADO!");
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