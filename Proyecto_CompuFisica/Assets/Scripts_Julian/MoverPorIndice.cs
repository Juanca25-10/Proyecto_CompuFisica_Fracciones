using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class JuegoTablero : MonoBehaviour
{
    [System.Serializable]
    public class DatosQuiz
    {
        [Tooltip("El número de casilla donde se activará esta pregunta")]
        public int numeroCasilla;

        [Header("Contenido del Contexto")]
        [TextArea(3, 6)]
        public string textoContexto;

        [Header("Contenido de la Pregunta")]
        [TextArea(2, 4)]
        public string textoPregunta;

        [Tooltip("Imagen opcional para ilustrar la pregunta en la UI")]
        public Sprite imagenAcompañante;

        [Header("Opciones de Respuesta (Texto)")]
        [Tooltip("Escribe aquí las 3 opciones. [0] = Texto para Z, [1] = Texto para X, [2] = Texto para C.")]
        public string[] opcionesTextos = new string[3];

        [Tooltip("Opción correcta: 0 para Z, 1 para X, 2 para C")]
        [Range(0, 2)] public int opcionCorrecta;
    }

    [Header("Jugadores")]
    public GameObject[] jugadores;
    public Animator[] animadoresJugadores;

    [Header("Sistema de Puntos")]
    [Tooltip("Arrastra aquí los TextMeshPro de puntaje de cada jugador (Elemento 0 = Jugador 1, Elemento 1 = Jugador 2)")]
    public TMP_Text[] textosPuntajesJugadores;
    private int[] puntajesJugadores;

    [Header("Cámaras Cinemachine Normales")]
    public GameObject[] camarasVirtualesJugadores;
    public GameObject camaraGlobalTablero;

    [Header("Cámaras Cinemachine de Quiz")]
    [Tooltip("La cámara de primer plano dedicada al Quiz (1 por jugador)")]
    public GameObject[] camarasQuizJugadores;

    [Tooltip("Tiempo en segundos que tarda la cámara en hacer la transición (Blend)")]
    public float tiempoTransicionCamara = 1f;

    [Header("Dados 3D de los Jugadores")]
    public GameObject[] dadosJugadores;

    [Header("Configuración del Dado")]
    public float tiempoGiroDado = 1.5f;
    public float tiempoEsperaResultado = 1f;
    public float offsetYPersonaje = 0f;

    [Tooltip("Rotaciones (Euler) para que cada cara mire a la cámara.")]
    public Vector3[] rotacionesCaras = new Vector3[6]
    {
        new Vector3(0, 0, 0), new Vector3(90, 0, 0), new Vector3(0, 90, 0),
        new Vector3(0, -90, 0), new Vector3(-90, 0, 0), new Vector3(180, 0, 0)
    };

    [Header("Posiciones (Se llenan solas)")]
    [SerializeField] private Transform[] posiciones;
    public float velocidadMovimiento = 4f;
    public float velocidadRotacion = 15f;

    [Header("Configuración del Sistema de Quiz")]
    public List<DatosQuiz> preguntasConfiguradas;

    [Header("UI del Quiz (Screen Space)")]
    public GameObject panelContextoUI;
    public TMP_Text textoContextoUI;
    public GameObject panelPreguntaUI;
    public TMP_Text textoPreguntaUI;
    public Image imagenPreguntaUI;

    [Tooltip("Arrastra aquí los 3 componentes de texto de la UI donde se mostrarán las opciones. [0]=Z, [1]=X, [2]=C")]
    public TMP_Text[] textosOpcionesUI = new TMP_Text[3];
    public GameObject[] resaltadosOpciones = new GameObject[3];

    [Header("Retroalimentación Visual (Alertas UI)")]
    [Tooltip("El panel/imagen que dice '¡Pregunta!' u '¡Ojo!' al caer en la casilla")]
    public GameObject alertaLlegadaPregunta;
    [Tooltip("El panel/imagen que dice '¡Correcto!'")]
    public GameObject alertaRespuestaCorrecta;
    [Tooltip("El panel/imagen que dice '¡Incorrecto!'")]
    public GameObject alertaRespuestaIncorrecta;
    [Tooltip("Cuánto tiempo en segundos dura la alerta en pantalla antes de avanzar")]
    public float tiempoMostrarAlertas = 2f;

    private const string PARAM_BLEND = "Blend";
    private int[] casillasActuales;
    private int turnoActual = 0;
    private bool juegoTerminado = false;
    private bool estaProcesandoTurno = false;

    private Vector3[] posicionesDeseadas;
    private bool[] estaMoviendose;

    private void Awake()
    {
        BuscarPosicionesAutomaticamente();

        if (jugadores != null && jugadores.Length > 0)
        {
            casillasActuales = new int[jugadores.Length];
            posicionesDeseadas = new Vector3[jugadores.Length];
            estaMoviendose = new bool[jugadores.Length];
            puntajesJugadores = new int[jugadores.Length];

            if (animadoresJugadores == null || animadoresJugadores.Length == 0)
            {
                animadoresJugadores = new Animator[jugadores.Length];
                for (int i = 0; i < jugadores.Length; i++)
                    if (jugadores[i] != null)
                        animadoresJugadores[i] = jugadores[i].GetComponentInChildren<Animator>();
            }
        }

        if (animadoresJugadores != null)
            foreach (Animator anim in animadoresJugadores)
                if (anim != null) anim.applyRootMotion = false;

        ApagarTodasLasCamarasEspeciales();
        ActualizarCamarasCinemachine();
        OcultarTodosLosDados();

        if (camaraGlobalTablero != null) camaraGlobalTablero.SetActive(false);

        // Ocultar todas las UI
        if (panelContextoUI != null) panelContextoUI.SetActive(false);
        if (panelPreguntaUI != null) panelPreguntaUI.SetActive(false);
        if (alertaLlegadaPregunta != null) alertaLlegadaPregunta.SetActive(false);
        if (alertaRespuestaCorrecta != null) alertaRespuestaCorrecta.SetActive(false);
        if (alertaRespuestaIncorrecta != null) alertaRespuestaIncorrecta.SetActive(false);

        ActualizarResaltadosUI(-1);
        PonerTodosEnIdle();
        ActualizarTextoPuntajesUI();
    }

    private void Start()
    {
        if (posiciones == null || posiciones.Length == 0 || jugadores == null) return;
        float alturaTablero = posiciones[0].position.y + offsetYPersonaje;
        for (int i = 0; i < jugadores.Length; i++)
        {
            if (jugadores[i] != null)
            {
                Vector3 pos = jugadores[i].transform.position;
                pos.y = alturaTablero;
                jugadores[i].transform.position = pos;
            }
        }
    }

    private void LateUpdate()
    {
        if (jugadores == null || posicionesDeseadas == null) return;
        for (int i = 0; i < jugadores.Length; i++)
        {
            if (estaMoviendose[i] && jugadores[i] != null)
                jugadores[i].transform.position = posicionesDeseadas[i];
        }
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
        if (!context.performed) return;
        LanzarDadoFisico();
    }

    public void LanzarDadoFisico()
    {
        if (juegoTerminado || estaProcesandoTurno) return;
        StartCoroutine(SecuenciaTurnoCompleta());
    }

    // =========================================================================
    // FLUJO PRINCIPAL DEL TURNO
    // =========================================================================

    private IEnumerator SecuenciaTurnoCompleta()
    {
        estaProcesandoTurno = true;

        GameObject dadoActual = dadosJugadores[turnoActual];
        if (dadoActual != null) dadoActual.SetActive(true);

        int resultadoDado = Random.Range(1, 7);
        Debug.Log($"🎲 Jugador {turnoActual + 1} está lanzando el dado...");

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

        if (dadoActual != null)
        {
            Quaternion rotacionIncomoda = dadoActual.transform.localRotation;
            Quaternion rotacionDestino = Quaternion.Euler(rotacionesCaras[resultadoDado - 1]);
            float tiempoFrenado = 0f;
            while (tiempoFrenado < 1f)
            {
                tiempoFrenado += Time.deltaTime / 0.4f;
                dadoActual.transform.localRotation = Quaternion.Slerp(rotacionIncomoda, rotacionDestino, tiempoFrenado);
                yield return null;
            }
            dadoActual.transform.localRotation = rotacionDestino;

            Vector3 escalaOriginal = dadoActual.transform.localScale;
            Vector3 escalaReducida = escalaOriginal * 0.5f;
            float tiempoRebote = 0f;
            while (tiempoRebote < 1f) { tiempoRebote += Time.deltaTime / 0.15f; dadoActual.transform.localScale = Vector3.Lerp(escalaOriginal, escalaReducida, tiempoRebote); yield return null; }
            tiempoRebote = 0f;
            while (tiempoRebote < 1f) { tiempoRebote += Time.deltaTime / 0.15f; dadoActual.transform.localScale = Vector3.Lerp(escalaReducida, escalaOriginal, tiempoRebote); yield return null; }
            dadoActual.transform.localScale = escalaOriginal;
        }

        yield return new WaitForSeconds(tiempoEsperaResultado);
        if (dadoActual != null) dadoActual.SetActive(false);

        int casillaAnterior = casillasActuales[turnoActual];
        int nuevaCasilla = casillaAnterior + resultadoDado;
        if (nuevaCasilla >= posiciones.Length) nuevaCasilla = posiciones.Length - 1;

        yield return StartCoroutine(MoverFichaPasoAPaso(turnoActual, casillaAnterior, nuevaCasilla));

        // --- SISTEMA DE QUIZ CON CÁMARAS Y TEXTOS ---
        DatosQuiz quizDeEstaCasilla = ObtenerQuizDeCasillaActual(casillasActuales[turnoActual]);
        if (quizDeEstaCasilla != null)
        {
            yield return StartCoroutine(ManejarSecuenciaQuizCinematico(quizDeEstaCasilla));
        }

        if (casillasActuales[turnoActual] >= posiciones.Length - 1)
        {
            Debug.Log($"🏆 ¡EL JUGADOR {turnoActual + 1} HA GANADO!");
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
        if (desdeCasilla == hastaCasilla) yield break;

        GameObject ficha = jugadores[jugadorIndice];
        estaMoviendose[jugadorIndice] = true;
        SetBlend(jugadorIndice, 1f);

        for (int c = desdeCasilla + 1; c <= hastaCasilla; c++)
        {
            Vector3 posInicio = posiciones[c - 1].position; posInicio.y += offsetYPersonaje;
            Vector3 posFin = posiciones[c].position; posFin.y += offsetYPersonaje;
            Vector3 direccion = posFin - posInicio; direccion.y = 0f;

            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Clamp01(t + Time.deltaTime * velocidadMovimiento);
                Vector3 posActual = Vector3.Lerp(posInicio, posFin, t);
                posicionesDeseadas[jugadorIndice] = posActual;

                if (direccion.sqrMagnitude > 0.001f)
                {
                    Quaternion rotObjetivo = Quaternion.LookRotation(direccion);
                    ficha.transform.rotation = Quaternion.Slerp(ficha.transform.rotation, rotObjetivo, Time.deltaTime * velocidadRotacion);
                }
                yield return null;
            }
            posicionesDeseadas[jugadorIndice] = posFin;
            ficha.transform.position = posFin;
            casillasActuales[jugadorIndice] = c;
        }

        SetBlend(jugadorIndice, 0f);
        estaMoviendose[jugadorIndice] = false;
    }

    // =========================================================================
    // LÓGICA DEL QUIZ (MODIFICADA PARA TEXTO EN UI)
    // =========================================================================

    private DatosQuiz ObtenerQuizDeCasillaActual(int numeroCasilla)
    {
        if (preguntasConfiguradas == null) return null;
        return preguntasConfiguradas.Find(q => q.numeroCasilla == numeroCasilla);
    }

    private IEnumerator ManejarSecuenciaQuizCinematico(DatosQuiz quiz)
    {
        // 1. Mostrar Alerta de Entrada JUSTO al caer
        yield return StartCoroutine(MostrarAlertaTemporal(alertaLlegadaPregunta));

        // 2. Cambiar a Cámara de Quiz (Primer plano)
        if (camarasVirtualesJugadores.Length > turnoActual && camarasVirtualesJugadores[turnoActual] != null)
            camarasVirtualesJugadores[turnoActual].SetActive(false);

        if (camarasQuizJugadores.Length > turnoActual && camarasQuizJugadores[turnoActual] != null)
            camarasQuizJugadores[turnoActual].SetActive(true);

        yield return new WaitForSeconds(2);

        // 3. Mostrar UI de Contexto
        if (panelContextoUI != null)
        {
            if (textoContextoUI != null) textoContextoUI.text = quiz.textoContexto;
            panelContextoUI.SetActive(true);
        }

        // Esperar Enter para avanzar del contexto a la pregunta
        yield return new WaitUntil(() => Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame);
        if (panelContextoUI != null) panelContextoUI.SetActive(false);
        yield return new WaitForSeconds(0.2f);

        // 4. Mostrar UI de Pregunta e inyectar los textos en los botones
        if (panelPreguntaUI != null)
        {
            if (textoPreguntaUI != null) textoPreguntaUI.text = quiz.textoPregunta;
            if (imagenPreguntaUI != null) imagenPreguntaUI.sprite = quiz.imagenAcompañante;

            // Inyectar los strings del inspector en los textos de la UI
            if (textosOpcionesUI != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (textosOpcionesUI.Length > i && textosOpcionesUI[i] != null && quiz.opcionesTextos.Length > i)
                    {
                        textosOpcionesUI[i].text = quiz.opcionesTextos[i];
                    }
                }
            }
            panelPreguntaUI.SetActive(true);
        }

        int opcionSeleccionada = -1;
        ActualizarResaltadosUI(opcionSeleccionada);
        bool respuestaConfirmada = false;

        // Bucle de selección por teclado
        while (!respuestaConfirmada)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.zKey.wasPressedThisFrame) { opcionSeleccionada = 0; ActualizarResaltadosUI(opcionSeleccionada); }
                if (Keyboard.current.xKey.wasPressedThisFrame) { opcionSeleccionada = 1; ActualizarResaltadosUI(opcionSeleccionada); }
                if (Keyboard.current.cKey.wasPressedThisFrame) { opcionSeleccionada = 2; ActualizarResaltadosUI(opcionSeleccionada); }

                if (Keyboard.current.enterKey.wasPressedThisFrame && opcionSeleccionada != -1)
                {
                    respuestaConfirmada = true;
                }
            }
            yield return null;
        }

        // Apagar paneles de preguntas
        if (panelPreguntaUI != null) panelPreguntaUI.SetActive(false);
        ActualizarResaltadosUI(-1);

        // 5. Evaluar respuesta
        bool respondioBien = (opcionSeleccionada == quiz.opcionCorrecta);

        if (respondioBien)
        {
            puntajesJugadores[turnoActual]++;
            ActualizarTextoPuntajesUI();
            yield return StartCoroutine(MostrarAlertaTemporal(alertaRespuestaCorrecta));
        }
        else
        {
            yield return StartCoroutine(MostrarAlertaTemporal(alertaRespuestaIncorrecta));
        }

        // 6. Volver a la cámara normal del jugador
        if (camarasQuizJugadores.Length > turnoActual && camarasQuizJugadores[turnoActual] != null)
            camarasQuizJugadores[turnoActual].SetActive(false);

        if (camarasVirtualesJugadores.Length > turnoActual && camarasVirtualesJugadores[turnoActual] != null)
            camarasVirtualesJugadores[turnoActual].SetActive(true);

        yield return new WaitForSeconds(tiempoTransicionCamara);

        // 7. Si acertó, avanzar las 3 casillas de bonificación
        if (respondioBien)
        {
            int casillaActual = casillasActuales[turnoActual];
            int casillaDestinoExtra = casillaActual + 3;
            if (casillaDestinoExtra >= posiciones.Length) casillaDestinoExtra = posiciones.Length - 1;

            yield return StartCoroutine(MoverFichaPasoAPaso(turnoActual, casillaActual, casillaDestinoExtra));
        }
    }

    private IEnumerator MostrarAlertaTemporal(GameObject panelAlerta)
    {
        if (panelAlerta != null)
        {
            panelAlerta.SetActive(true);
            yield return new WaitForSeconds(tiempoMostrarAlertas);
            panelAlerta.SetActive(false);
        }
    }

    private void ActualizarResaltadosUI(int indiceSeleccionado)
    {
        if (resaltadosOpciones == null || resaltadosOpciones.Length < 3) return;
        for (int i = 0; i < 3; i++)
            if (resaltadosOpciones[i] != null)
                resaltadosOpciones[i].SetActive(i == indiceSeleccionado);
    }

    private void ActualizarTextoPuntajesUI()
    {
        if (textosPuntajesJugadores == null) return;
        for (int i = 0; i < textosPuntajesJugadores.Length; i++)
        {
            if (textosPuntajesJugadores[i] != null && i < puntajesJugadores.Length)
            {
                textosPuntajesJugadores[i].text = puntajesJugadores[i].ToString();
            }
        }
    }

    // =========================================================================

    private void SetBlend(int jugadorIndice, float valor)
    {
        Animator anim = ObtenerAnimador(jugadorIndice);
        if (anim != null) anim.SetFloat(PARAM_BLEND, valor);
    }

    private Animator ObtenerAnimador(int indice)
    {
        if (animadoresJugadores == null || indice >= animadoresJugadores.Length) return null;
        return animadoresJugadores[indice];
    }

    private void PonerTodosEnIdle()
    {
        if (animadoresJugadores == null) return;
        for (int i = 0; i < animadoresJugadores.Length; i++)
            SetBlend(i, 0f);
    }

    private void CambiarTurno()
    {
        turnoActual = (turnoActual + 1) % jugadores.Length;
        ApagarTodasLasCamarasEspeciales();
        ActualizarCamarasCinemachine();
    }

    private void ApagarTodasLasCamarasEspeciales()
    {
        if (camarasQuizJugadores != null)
            foreach (var cam in camarasQuizJugadores) if (cam != null) cam.SetActive(false);
    }

    private void ActualizarCamarasCinemachine()
    {
        if (camarasVirtualesJugadores == null || camarasVirtualesJugadores.Length == 0) return;

        for (int i = 0; i < camarasVirtualesJugadores.Length; i++)
            if (camarasVirtualesJugadores[i] != null)
                camarasVirtualesJugadores[i].SetActive(i == turnoActual);
    }

    private void OcultarTodosLosDados()
    {
        if (dadosJugadores == null) return;
        foreach (GameObject dado in dadosJugadores)
            if (dado != null) dado.SetActive(false);
    }
}