using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class JuegoTablero : MonoBehaviour
{
    [Header("Jugadores")]
    public GameObject[] jugadores;

    [Header("Cámaras Virtuales de Cinemachine")]
    public GameObject[] camarasVirtuales;

    [Header("Dados 3D de los Jugadores")]
    public GameObject[] dadosJugadores;

    [Header("Configuración del Dado")]
    public float tiempoGiroDado = 1.5f;
    public float tiempoEsperaResultado = 1f;

    [Tooltip("Rotaciones (Euler) para que cada cara (1 a 6) mire directamente a la cámara.")]
    public Vector3[] rotacionesCaras = new Vector3[6]
    {
        new Vector3(0, 0, 0),       // Cara 1
        new Vector3(90, 0, 0),      // Cara 2
        new Vector3(0, 90, 0),      // Cara 3
        new Vector3(0, -90, 0),     // Cara 4
        new Vector3(-90, 0, 0),     // Cara 5
        new Vector3(180, 0, 0)      // Cara 6 (Ajusta estos ángulos según tu modelo)
    };

    [Header("Posiciones (Se llenan solas)")]
    [SerializeField] private Transform[] posiciones;

    [Header("Configuración del Movimiento Ficha")]
    public float velocidadMovimiento = 4f;
    public float alturaSalto = 1.2f;

    private int[] casillasActuales;
    private int turnoActual = 0;
    private bool juegoTerminado = false;
    private bool estaProcesandoTurno = false;

    private void Awake()
    {
        BuscarPosicionesAutomaticamente();

        if (jugadores != null && jugadores.Length > 0)
        {
            casillasActuales = new int[jugadores.Length];
        }

        ActualizarCamarasCinemachine();
        OcultarTodosLosDados();
    }

    private void BuscarPosicionesAutomaticamente()
    {
        List<Transform> listaTemporal = new List<Transform>();
        int i = 0;
        if (GameObject.Find("pos0") == null) i = 1;

        while (true)
        {
            GameObject go = GameObject.Find("pos" + i);
            if (go != null) { listaTemporal.Add(go.transform); i++; }
            else break;
        }
        posiciones = listaTemporal.ToArray();
    }

    public void TurnoAvanzar(InputAction.CallbackContext context)
    {
        if (!context.performed || juegoTerminado || estaProcesandoTurno) return;
        StartCoroutine(SecuenciaTurnoCompleta());
    }

    private IEnumerator SecuenciaTurnoCompleta()
    {
        estaProcesandoTurno = true;

        // 1. Mostrar el dado del jugador actual
        GameObject dadoActual = dadosJugadores[turnoActual];
        if (dadoActual != null) dadoActual.SetActive(true);

        // 2. Lanzar el dado matemáticamente
        int resultadoDado = Random.Range(1, 7);
        Debug.Log($"🎲 <b>Jugador {turnoActual + 1}</b> está lanzando el dado...");

        // =========================================================================
        // 3. ANIMACIÓN DE ROTACIÓN POR TIEMPO (GIRO CAÓTICO)
        // =========================================================================
        float velocidadX = Random.Range(600f, 1200f) * (Random.value > 0.5f ? 1 : -1);
        float velocidadY = Random.Range(600f, 1200f) * (Random.value > 0.5f ? 1 : -1);
        float velocidadZ = Random.Range(600f, 1200f) * (Random.value > 0.5f ? 1 : -1);

        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < tiempoGiroDado)
        {
            if (dadoActual != null)
            {
                dadoActual.transform.Rotate(Vector3.up * velocidadY * Time.deltaTime, Space.World);
                dadoActual.transform.Rotate(new Vector3(velocidadX, 0, velocidadZ) * Time.deltaTime, Space.Self);
            }
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        // =========================================================================
        // 4. FRENADO Y ALINEACIÓN SUAVE A LA CARA FINAL (SLERP)
        // =========================================================================
        if (dadoActual != null)
        {
            Quaternion rotacionIncomoda = dadoActual.transform.localRotation;
            Quaternion rotacionDestino = Quaternion.Euler(rotacionesCaras[resultadoDado - 1]);

            float tiempoFrenado = 0f;
            float duracionFrenado = 0.4f; // Qué tan rápido se alinea

            while (tiempoFrenado < 1f)
            {
                tiempoFrenado += Time.deltaTime / duracionFrenado;
                dadoActual.transform.localRotation = Quaternion.Slerp(rotacionIncomoda, rotacionDestino, tiempoFrenado);
                yield return null;
            }
            dadoActual.transform.localRotation = rotacionDestino; // Asegurar posición exacta

            // =========================================================================
            // NUEVO AJUSTE: EFECTO REBOTE (ESCALA)
            // =========================================================================
            // Capturamos la escala actual del dado, por si el modelo no tiene escala 1,1,1
            Vector3 escalaOriginal = dadoActual.transform.localScale;
            Vector3 escalaReducida = escalaOriginal * 0.5f; // Encoger a la mitad

            float tiempoRebote = 0f;
            float duracionMediaRebote = 0.15f; // Tardará 0.15s en encogerse, y 0.15s en agrandarse.

            // Fase 1: Encogerse suavemente
            while (tiempoRebote < 1f)
            {
                tiempoRebote += Time.deltaTime / duracionMediaRebote;
                dadoActual.transform.localScale = Vector3.Lerp(escalaOriginal, escalaReducida, tiempoRebote);
                yield return null;
            }

            // Fase 2: Agrandarse de nuevo suavemente
            tiempoRebote = 0f; // Reiniciamos el contador de tiempo
            while (tiempoRebote < 1f)
            {
                tiempoRebote += Time.deltaTime / duracionMediaRebote;
                dadoActual.transform.localScale = Vector3.Lerp(escalaReducida, escalaOriginal, tiempoRebote);
                yield return null;
            }

            // Nos aseguramos de restaurar la escala exacta por si acaso
            dadoActual.transform.localScale = escalaOriginal;
            // =========================================================================
        }

        Debug.Log($"🎲 ¡Salió un <b>{resultadoDado}</b>!");

        // 5. Esperar un momento para que el jugador vea el resultado
        yield return new WaitForSeconds(tiempoEsperaResultado);

        // 6. Ocultar el dado
        if (dadoActual != null) dadoActual.SetActive(false);

        // 7. Calcular y ejecutar el movimiento de la ficha
        int casillaAnterior = casillasActuales[turnoActual];
        int nuevaCasilla = casillaAnterior + resultadoDado;

        if (nuevaCasilla >= posiciones.Length) nuevaCasilla = posiciones.Length - 1;

        yield return StartCoroutine(MoverFichaPasoAPaso(turnoActual, casillaAnterior, nuevaCasilla));

        // 8. Finalizar turno y cambiar al siguiente jugador
        if (casillasActuales[turnoActual] >= posiciones.Length - 1)
        {
            Debug.Log($"🏆 <b>¡EL JUGADOR {turnoActual + 1} HA GANADO!</b>");
            juegoTerminado = true;
        }
        else
        {
            CambiarTurno();
        }

        estaProcesandoTurno = false;
    }

    private IEnumerator MoverFichaPasoAPaso(int jugadorIndice, int desdeCasilla, int hastaCasilla)
    {
        GameObject ficha = jugadores[jugadorIndice];

        for (int c = desdeCasilla + 1; c <= hastaCasilla; c++)
        {
            Vector3 posInicio = ficha.transform.position;
            Vector3 posFin = posiciones[c].position;
            float t = 0;

            while (t < 1f)
            {
                t += Time.deltaTime * velocidadMovimiento;
                Vector3 posActual = Vector3.Lerp(posInicio, posFin, t);
                posActual.y += Mathf.Sin(t * Mathf.PI) * alturaSalto;
                ficha.transform.position = posActual;
                yield return null;
            }

            ficha.transform.position = posFin;
            casillasActuales[jugadorIndice] = c;
        }
    }

    private void CambiarTurno()
    {
        turnoActual = (turnoActual + 1) % jugadores.Length;
        ActualizarCamarasCinemachine();
        Debug.Log($"👉 Turno del <b>Jugador {turnoActual + 1}</b>. ¡Presiona 'E' para tirar!");
    }

    private void ActualizarCamarasCinemachine()
    {
        if (camarasVirtuales == null || camarasVirtuales.Length == 0) return;
        for (int i = 0; i < camarasVirtuales.Length; i++)
        {
            if (camarasVirtuales[i] != null) camarasVirtuales[i].SetActive(i == turnoActual);
        }
    }

    private void OcultarTodosLosDados()
    {
        if (dadosJugadores == null) return;
        foreach (GameObject dado in dadosJugadores)
        {
            if (dado != null) dado.SetActive(false);
        }
    }
}