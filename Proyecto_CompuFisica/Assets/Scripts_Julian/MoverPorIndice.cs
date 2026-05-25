using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class JuegoTablero : MonoBehaviour
{
    public static JuegoTablero Instancia; // <--- SINGLETON AUTOMÁTICO

    [System.Serializable]
    public class DatosQuiz
    {
        public int numeroCasilla;
        [TextArea(3, 6)] public string textoContexto;
        [TextArea(2, 4)] public string textoPregunta;
        public Sprite imagenAcompañante;
        public string[] opcionesTextos = new string[3];
        [Range(0, 2)] public int opcionCorrecta;
    }

    [Header("Jugadores y UI")]
    public GameObject[] jugadores;
    public Animator[] animadoresJugadores;
    public TMP_Text[] textosPuntajesJugadores;
    private int[] puntajesJugadores;

    [Header("Cámaras")]
    public GameObject[] camarasVirtualesJugadores;
    public GameObject camaraGlobalTablero;
    public GameObject[] camarasQuizJugadores;
    public float tiempoTransicionCamara = 1f;

    [Header("Dados y Movimiento")]
    public GameObject[] dadosJugadores;
    public float tiempoGiroDado = 1.5f;
    public float tiempoEsperaResultado = 1f;
    public float offsetYPersonaje = 0f;
    public Vector3[] rotacionesCaras = new Vector3[6] { new Vector3(0, 0, 0), new Vector3(90, 0, 0), new Vector3(0, 90, 0), new Vector3(0, -90, 0), new Vector3(-90, 0, 0), new Vector3(180, 0, 0) };
    [SerializeField] private Transform[] posiciones;
    public float velocidadMovimiento = 4f;
    public float velocidadRotacion = 15f;

    [Header("Config Quiz y Victoria")]
    public List<DatosQuiz> preguntasConfiguradas;
    public GameObject panelContextoUI; public TMP_Text textoContextoUI;
    public GameObject panelPreguntaUI; public TMP_Text textoPreguntaUI; public Image imagenPreguntaUI;
    public TMP_Text[] textosOpcionesUI = new TMP_Text[3];
    public GameObject[] resaltadosOpciones = new GameObject[3];
    public GameObject alertaLlegadaPregunta; public GameObject alertaRespuestaCorrecta; public GameObject alertaRespuestaIncorrecta;
    public float tiempoMostrarAlertas = 2f;
    public GameObject panelVictoriaUI; public TMP_Text textoVictoriaUI;

    private const string PARAM_BLEND = "Blend";
    private int[] casillasActuales; private int turnoActual = 0;
    private bool juegoTerminado = false; private bool estaProcesandoTurno = false; private bool enPantallaQuiz = false;
    private Vector3[] posicionesDeseadas; private bool[] estaMoviendose;

    // Variables puente del Arduino
    private bool bRojo = false; private bool bAzul = false; private bool bVerde = false; private bool bBlanco = false;

    public void PresionarFisicoRojo() { bRojo = true; }
    public void PresionarFisicoAzul() { bAzul = true; }
    public void PresionarFisicoVerde() { bVerde = true; }
    public void PresionarFisicoBlanco() { bBlanco = true; }

    private void Awake()
    {
        Instancia = this; // Se auto-asigna

        BuscarPosicionesAutomaticamente();
        if (jugadores != null && jugadores.Length > 0)
        {
            casillasActuales = new int[jugadores.Length]; posicionesDeseadas = new Vector3[jugadores.Length];
            estaMoviendose = new bool[jugadores.Length]; puntajesJugadores = new int[jugadores.Length];
            if (animadoresJugadores == null || animadoresJugadores.Length == 0)
            {
                animadoresJugadores = new Animator[jugadores.Length];
                for (int i = 0; i < jugadores.Length; i++) if (jugadores[i] != null) animadoresJugadores[i] = jugadores[i].GetComponentInChildren<Animator>();
            }
        }
        if (animadoresJugadores != null) foreach (Animator anim in animadoresJugadores) if (anim != null) anim.applyRootMotion = false;

        ApagarTodasLasCamarasEspeciales(); ActualizarCamarasCinemachine(); OcultarTodosLosDados();
        if (camaraGlobalTablero != null) camaraGlobalTablero.SetActive(false);
        if (panelContextoUI != null) panelContextoUI.SetActive(false);
        if (panelPreguntaUI != null) panelPreguntaUI.SetActive(false);
        if (alertaLlegadaPregunta != null) alertaLlegadaPregunta.SetActive(false);
        if (alertaRespuestaCorrecta != null) alertaRespuestaCorrecta.SetActive(false);
        if (alertaRespuestaIncorrecta != null) alertaRespuestaIncorrecta.SetActive(false);
        if (panelVictoriaUI != null) panelVictoriaUI.SetActive(false);
        ActualizarResaltadosUI(-1); PonerTodosEnIdle(); ActualizarTextoPuntajesUI();
    }

    private void Start()
    {
        if (posiciones == null || posiciones.Length == 0 || jugadores == null) return;
        float alturaTablero = posiciones[0].position.y + offsetYPersonaje;
        for (int i = 0; i < jugadores.Length; i++) { if (jugadores[i] != null) { Vector3 pos = jugadores[i].transform.position; pos.y = alturaTablero; jugadores[i].transform.position = pos; } }
    }

    private void Update()
    {
        if (juegoTerminado || estaProcesandoTurno || enPantallaQuiz) return;

        bool presionoBlanco = (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame) || bBlanco;
        bool presionoAzul = (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame) || bAzul;
        bool presionoRojo = (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame) || bRojo;
        bool presionoVerde = (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame) || bVerde;

        if (presionoBlanco) LanzarDadoFisico();
        if (presionoAzul) AlternarCamaraManual();
        if (presionoRojo) CelebrarJugadorActual();
        if (presionoVerde) AlternarPausa();
    }

    private void LateUpdate()
    {
        if (jugadores != null && posicionesDeseadas != null)
            for (int i = 0; i < jugadores.Length; i++) if (estaMoviendose[i] && jugadores[i] != null) jugadores[i].transform.position = posicionesDeseadas[i];

        bRojo = false; bAzul = false; bVerde = false; bBlanco = false;
    }

    private void BuscarPosicionesAutomaticamente()
    {
        List<Transform> listaTemporal = new List<Transform>(); int i = 0;
        if (GameObject.Find("pos0") == null) i = 1;
        while (true) { GameObject go = GameObject.Find("pos" + i); if (go != null) { listaTemporal.Add(go.transform); i++; } else break; }
        posiciones = listaTemporal.ToArray();
    }

    public void LanzarDadoFisico() { if (!juegoTerminado && !estaProcesandoTurno && !enPantallaQuiz) StartCoroutine(SecuenciaTurnoCompleta()); }

    private IEnumerator SecuenciaTurnoCompleta()
    {
        estaProcesandoTurno = true;
        GameObject dadoActual = dadosJugadores[turnoActual];
        if (dadoActual != null) dadoActual.SetActive(true);
        int resultadoDado = Random.Range(1, 7);
        float velocidadX = Random.Range(600f, 1200f) * (Random.value > 0.5f ? 1 : -1); float velocidadY = Random.Range(600f, 1200f) * (Random.value > 0.5f ? 1 : -1); float velocidadZ = Random.Range(600f, 1200f) * (Random.value > 0.5f ? 1 : -1);

        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < tiempoGiroDado)
        {
            if (dadoActual != null) { dadoActual.transform.Rotate(Vector3.up * velocidadY * Time.deltaTime, Space.World); dadoActual.transform.Rotate(new Vector3(velocidadX, 0, velocidadZ) * Time.deltaTime, Space.Self); }
            tiempoTranscurrido += Time.deltaTime; yield return null;
        }

        if (dadoActual != null)
        {
            Quaternion rotIncomoda = dadoActual.transform.localRotation; Quaternion rotDestino = Quaternion.Euler(rotacionesCaras[resultadoDado - 1]);
            float t = 0f; while (t < 1f) { t += Time.deltaTime / 0.4f; dadoActual.transform.localRotation = Quaternion.Slerp(rotIncomoda, rotDestino, t); yield return null; }
            dadoActual.transform.localRotation = rotDestino;
            Vector3 escOriginal = dadoActual.transform.localScale; Vector3 escReducida = escOriginal * 0.5f;
            t = 0f; while (t < 1f) { t += Time.deltaTime / 0.15f; dadoActual.transform.localScale = Vector3.Lerp(escOriginal, escReducida, t); yield return null; }
            t = 0f; while (t < 1f) { t += Time.deltaTime / 0.15f; dadoActual.transform.localScale = Vector3.Lerp(escReducida, escOriginal, t); yield return null; }
            dadoActual.transform.localScale = escOriginal;
        }

        yield return new WaitForSeconds(tiempoEsperaResultado);
        if (dadoActual != null) dadoActual.SetActive(false);

        int casillaAnterior = casillasActuales[turnoActual]; int nuevaCasilla = casillaAnterior + resultadoDado;
        if (nuevaCasilla >= posiciones.Length) nuevaCasilla = posiciones.Length - 1;

        yield return StartCoroutine(MoverFichaPasoAPaso(turnoActual, casillaAnterior, nuevaCasilla));

        DatosQuiz quizDeEstaCasilla = ObtenerQuizDeCasillaActual(casillasActuales[turnoActual]);
        if (quizDeEstaCasilla != null) yield return StartCoroutine(ManejarSecuenciaQuizCinematico(quizDeEstaCasilla));

        if (casillasActuales[turnoActual] >= posiciones.Length - 1)
        {
            juegoTerminado = true; StartCoroutine(MostrarMenuVictoria());
        }
        else { CambiarTurno(); }

        estaProcesandoTurno = false;
    }

    private IEnumerator MoverFichaPasoAPaso(int jugadorIndice, int desdeCasilla, int hastaCasilla)
    {
        if (desdeCasilla == hastaCasilla) yield break;
        GameObject ficha = jugadores[jugadorIndice]; estaMoviendose[jugadorIndice] = true; SetBlend(jugadorIndice, 1f);

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
                if (direccion.sqrMagnitude > 0.001f) ficha.transform.rotation = Quaternion.Slerp(ficha.transform.rotation, Quaternion.LookRotation(direccion), Time.deltaTime * velocidadRotacion);
                yield return null;
            }
            posicionesDeseadas[jugadorIndice] = posFin; ficha.transform.position = posFin; casillasActuales[jugadorIndice] = c;
        }
        SetBlend(jugadorIndice, 0f); estaMoviendose[jugadorIndice] = false;
    }

    private DatosQuiz ObtenerQuizDeCasillaActual(int num) { if (preguntasConfiguradas == null) return null; return preguntasConfiguradas.Find(q => q.numeroCasilla == num); }

    private IEnumerator ManejarSecuenciaQuizCinematico(DatosQuiz quiz)
    {
        enPantallaQuiz = true;
        yield return StartCoroutine(MostrarAlertaTemporal(alertaLlegadaPregunta));
        if (camarasVirtualesJugadores.Length > turnoActual && camarasVirtualesJugadores[turnoActual] != null) camarasVirtualesJugadores[turnoActual].SetActive(false);
        if (camarasQuizJugadores.Length > turnoActual && camarasQuizJugadores[turnoActual] != null) camarasQuizJugadores[turnoActual].SetActive(true);
        yield return new WaitForSeconds(2);

        if (panelContextoUI != null) { if (textoContextoUI != null) textoContextoUI.text = quiz.textoContexto; panelContextoUI.SetActive(true); }

        yield return new WaitUntil(() => (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame) || bBlanco);
        if (panelContextoUI != null) panelContextoUI.SetActive(false); yield return new WaitForSeconds(0.2f);

        if (panelPreguntaUI != null)
        {
            if (textoPreguntaUI != null) textoPreguntaUI.text = quiz.textoPregunta;
            if (imagenPreguntaUI != null) imagenPreguntaUI.sprite = quiz.imagenAcompañante;
            if (textosOpcionesUI != null) for (int i = 0; i < 3; i++) if (textosOpcionesUI.Length > i && textosOpcionesUI[i] != null && quiz.opcionesTextos.Length > i) textosOpcionesUI[i].text = quiz.opcionesTextos[i];
            panelPreguntaUI.SetActive(true);
        }

        int opcionSeleccionada = -1; ActualizarResaltadosUI(opcionSeleccionada); bool respuestaConfirmada = false;

        while (!respuestaConfirmada)
        {
            bool presionoRojo = (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame) || bRojo;
            bool presionoAzul = (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame) || bAzul;
            bool presionoVerde = (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame) || bVerde;
            bool presionoBlanco = (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame) || bBlanco;

            if (presionoRojo) { opcionSeleccionada = 0; ActualizarResaltadosUI(opcionSeleccionada); }
            if (presionoAzul) { opcionSeleccionada = 1; ActualizarResaltadosUI(opcionSeleccionada); }
            if (presionoVerde) { opcionSeleccionada = 2; ActualizarResaltadosUI(opcionSeleccionada); }
            if (presionoBlanco && opcionSeleccionada != -1) respuestaConfirmada = true;
            yield return null;
        }

        if (panelPreguntaUI != null) panelPreguntaUI.SetActive(false); ActualizarResaltadosUI(-1);
        bool respondioBien = (opcionSeleccionada == quiz.opcionCorrecta);

        if (respondioBien) { puntajesJugadores[turnoActual]++; ActualizarTextoPuntajesUI(); yield return StartCoroutine(MostrarAlertaTemporal(alertaRespuestaCorrecta)); }
        else { yield return StartCoroutine(MostrarAlertaTemporal(alertaRespuestaIncorrecta)); }

        if (camarasQuizJugadores.Length > turnoActual && camarasQuizJugadores[turnoActual] != null) camarasQuizJugadores[turnoActual].SetActive(false);
        if (camarasVirtualesJugadores.Length > turnoActual && camarasVirtualesJugadores[turnoActual] != null) camarasVirtualesJugadores[turnoActual].SetActive(true);
        yield return new WaitForSeconds(tiempoTransicionCamara);

        if (respondioBien)
        {
            int casActual = casillasActuales[turnoActual]; int casDestino = casActual + 3;
            if (casDestino >= posiciones.Length) casDestino = posiciones.Length - 1;
            yield return StartCoroutine(MoverFichaPasoAPaso(turnoActual, casActual, casDestino));
        }
        enPantallaQuiz = false;
    }

    private void AlternarCamaraManual()
    {
        if (camaraGlobalTablero != null && camarasVirtualesJugadores != null && camarasVirtualesJugadores.Length > turnoActual)
        {
            bool vistaGlobal = camaraGlobalTablero.activeSelf; camaraGlobalTablero.SetActive(!vistaGlobal);
            if (camarasVirtualesJugadores[turnoActual] != null) camarasVirtualesJugadores[turnoActual].SetActive(vistaGlobal);
        }
    }

    private void CelebrarJugadorActual() { Animator anim = ObtenerAnimador(turnoActual); if (anim != null) anim.SetTrigger("Celebrar"); }
    private void AlternarPausa() { MenuManager manager = FindObjectOfType<MenuManager>(); if (manager != null) manager.AlternarMenu(); }

    private IEnumerator MostrarAlertaTemporal(GameObject panel) { if (panel != null) { panel.SetActive(true); yield return new WaitForSeconds(tiempoMostrarAlertas); panel.SetActive(false); } }
    private void ActualizarResaltadosUI(int idx) { if (resaltadosOpciones == null) return; for (int i = 0; i < 3; i++) if (resaltadosOpciones[i] != null) resaltadosOpciones[i].SetActive(i == idx); }
    private void ActualizarTextoPuntajesUI() { if (textosPuntajesJugadores == null) return; for (int i = 0; i < textosPuntajesJugadores.Length; i++) if (textosPuntajesJugadores[i] != null && i < puntajesJugadores.Length) textosPuntajesJugadores[i].text = puntajesJugadores[i].ToString(); }
    private void SetBlend(int idx, float val) { Animator anim = ObtenerAnimador(idx); if (anim != null) anim.SetFloat(PARAM_BLEND, val); }
    private Animator ObtenerAnimador(int idx) { if (animadoresJugadores == null || idx >= animadoresJugadores.Length) return null; return animadoresJugadores[idx]; }
    private void PonerTodosEnIdle() { if (animadoresJugadores == null) return; for (int i = 0; i < animadoresJugadores.Length; i++) SetBlend(i, 0f); }
    private void CambiarTurno() { turnoActual = (turnoActual + 1) % jugadores.Length; ApagarTodasLasCamarasEspeciales(); ActualizarCamarasCinemachine(); }
    private void ApagarTodasLasCamarasEspeciales() { if (camarasQuizJugadores != null) foreach (var cam in camarasQuizJugadores) if (cam != null) cam.SetActive(false); }
    private void ActualizarCamarasCinemachine() { if (camarasVirtualesJugadores == null) return; for (int i = 0; i < camarasVirtualesJugadores.Length; i++) if (camarasVirtualesJugadores[i] != null) camarasVirtualesJugadores[i].SetActive(i == turnoActual); }
    private void OcultarTodosLosDados() { if (dadosJugadores == null) return; foreach (GameObject dado in dadosJugadores) if (dado != null) dado.SetActive(false); }

    private IEnumerator MostrarMenuVictoria()
    {
        if (panelVictoriaUI != null) panelVictoriaUI.SetActive(true);
        if (textoVictoriaUI != null) textoVictoriaUI.text = $"¡EL JUGADOR {turnoActual + 1}\nHA GANADO!";

        while (true)
        {
            bool presionoRojo = (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame) || bRojo;
            bool presionoAzul = (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame) || bAzul;
            bool presionoVerde = (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame) || bVerde;
            bool presionoBlanco = (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame) || bBlanco;

            if (presionoRojo) { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); yield break; }
            if (presionoAzul) { SceneManager.LoadScene("MenuPrincipal"); yield break; }
            if (presionoVerde) { Application.Quit(); yield break; }
            if (presionoBlanco) { if (panelVictoriaUI != null) panelVictoriaUI.SetActive(!panelVictoriaUI.activeSelf); }
            yield return null;
        }
    }
}