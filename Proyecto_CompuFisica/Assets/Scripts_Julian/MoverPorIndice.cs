using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class JuegoTablero : MonoBehaviour
{
    [Header("Jugadores")]
    public GameObject[] jugadores;

    [Header("Animadores de los Jugadores")]
    [Tooltip("Se auto-detectan desde los jugadores si se deja vacío")]
    public Animator[] animadoresJugadores;

    [Header("Cámaras Virtuales de Cinemachine")]
    public GameObject[] camarasVirtuales;

    [Header("Dados 3D de los Jugadores")]
    public GameObject[] dadosJugadores;

    [Header("Configuración del Dado")]
    public float tiempoGiroDado = 1.5f;
    public float tiempoEsperaResultado = 1f;

    [Tooltip("Ajuste vertical sobre las casillas. Negativo si el personaje flota, positivo si se hunde.")]
    public float offsetYPersonaje = 0f;

    [Tooltip("Rotaciones (Euler) para que cada cara (1 a 6) mire directamente a la cámara.")]
    public Vector3[] rotacionesCaras = new Vector3[6]
    {
        new Vector3(0, 0, 0),       // Cara 1
        new Vector3(90, 0, 0),      // Cara 2
        new Vector3(0, 90, 0),      // Cara 3
        new Vector3(0, -90, 0),     // Cara 4
        new Vector3(-90, 0, 0),     // Cara 5
        new Vector3(180, 0, 0)      // Cara 6
    };

    [Header("Posiciones (Se llenan solas)")]
    [SerializeField] private Transform[] posiciones;

    [Header("Configuración del Movimiento Ficha")]
    public float velocidadMovimiento = 4f;
    public float alturaSalto = 1.2f;

    [Header("Configuración de Animación del Personaje")]
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que el personaje SALTE en vez de CORRER al pasar cada casilla (0 = nunca, 1 = siempre)")]
    public float probabilidadSalto = 0.3f;

    [Tooltip("Qué tan rápido rota el personaje para mirar hacia donde camina")]
    public float velocidadRotacion = 15f;

   
    private const string PARAM_BLEND = "Blend";   
    private const string PARAM_SALTAR = "Saltar";  

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

        ActualizarCamarasCinemachine();
        OcultarTodosLosDados();
        PonerTodosEnIdle();
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

        // ── Frenado y alineación a la cara final ─────────────────────────
        if (dadoActual != null)
        {
            Quaternion rotacionIncomoda = dadoActual.transform.localRotation;
            Quaternion rotacionDestino = Quaternion.Euler(rotacionesCaras[resultadoDado - 1]);

            float tiempoFrenado = 0f;
            float duracionFrenado = 0.4f;

            while (tiempoFrenado < 1f)
            {
                tiempoFrenado += Time.deltaTime / duracionFrenado;
                dadoActual.transform.localRotation = Quaternion.Slerp(rotacionIncomoda, rotacionDestino, tiempoFrenado);
                yield return null;
            }
            dadoActual.transform.localRotation = rotacionDestino;

           
            Vector3 escalaOriginal = dadoActual.transform.localScale;
            Vector3 escalaReducida = escalaOriginal * 0.5f;
            float tiempoRebote = 0f;
            float duracionRebote = 0.15f;

            while (tiempoRebote < 1f)
            {
                tiempoRebote += Time.deltaTime / duracionRebote;
                dadoActual.transform.localScale = Vector3.Lerp(escalaOriginal, escalaReducida, tiempoRebote);
                yield return null;
            }
            tiempoRebote = 0f;
            while (tiempoRebote < 1f)
            {
                tiempoRebote += Time.deltaTime / duracionRebote;
                dadoActual.transform.localScale = Vector3.Lerp(escalaReducida, escalaOriginal, tiempoRebote);
                yield return null;
            }
            dadoActual.transform.localScale = escalaOriginal;
        }

        Debug.Log($"🎲 ¡Salió un <b>{resultadoDado}</b>!");

        // 3. Pausa para que el jugador vea el resultado
        yield return new WaitForSeconds(tiempoEsperaResultado);

        // 4. Ocultar dado
        if (dadoActual != null) dadoActual.SetActive(false);

        // 5. Mover la ficha animada
        int casillaAnterior = casillasActuales[turnoActual];
        int nuevaCasilla = casillaAnterior + resultadoDado;
        if (nuevaCasilla >= posiciones.Length) nuevaCasilla = posiciones.Length - 1;

        yield return StartCoroutine(MoverFichaPasoAPaso(turnoActual, casillaAnterior, nuevaCasilla));

        // 6. Volver a Idle al terminar el recorrido
        SetBlend(turnoActual, 0f);

        // 7. Comprobar victoria o cambiar turno
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
        estaMoviendose[jugadorIndice] = true;   // ← ACTIVAR

        for (int c = desdeCasilla + 1; c <= hastaCasilla; c++)
        {
            Vector3 posInicio = ficha.transform.position;
            Vector3 posFin = posiciones[c].position;
            posFin.y += offsetYPersonaje;

            Vector3 direccion = posiciones[c].position - posInicio;
            direccion.y = 0f;

            bool esSalto = Random.value < probabilidadSalto;
            if (esSalto) SetTrigger(jugadorIndice, PARAM_SALTAR);
            else SetBlend(jugadorIndice, 1f);

            float t = 0f;
            
            while (t < 1f)
            {
                t = Mathf.Clamp01(t + Time.deltaTime * velocidadMovimiento);

                // Guardamos en posicionesDeseadas → LateUpdate la aplica tras el Animator
                posicionesDeseadas[jugadorIndice] = Vector3.Lerp(posInicio, posFin, t);


                if (direccion.sqrMagnitude > 0.001f)
                {
                    Quaternion rotObjetivo = Quaternion.LookRotation(direccion);
                    ficha.transform.rotation = Quaternion.Slerp(
                        ficha.transform.rotation, rotObjetivo,
                        Time.deltaTime * velocidadRotacion);
                }

                yield return null;
            }

            posicionesDeseadas[jugadorIndice] = posFin;
            ficha.transform.position = posFin;
            casillasActuales[jugadorIndice] = c;
        }

        estaMoviendose[jugadorIndice] = false;  
    }

 

    private void SetBlend(int jugadorIndice, float valor)
    {
        Animator anim = ObtenerAnimador(jugadorIndice);
        if (anim != null) anim.SetFloat(PARAM_BLEND, valor);
    }

    private void SetTrigger(int jugadorIndice, string trigger)
    {
        Animator anim = ObtenerAnimador(jugadorIndice);
        if (anim != null) anim.SetTrigger(trigger);
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
        ActualizarCamarasCinemachine();
        Debug.Log($"👉 Turno del <b>Jugador {turnoActual + 1}</b>. ¡Presiona 'E' para tirar!");
    }

    private void ActualizarCamarasCinemachine()
    {
        if (camarasVirtuales == null || camarasVirtuales.Length == 0) return;
        for (int i = 0; i < camarasVirtuales.Length; i++)
        {
            if (camarasVirtuales[i] != null)
                camarasVirtuales[i].SetActive(i == turnoActual);
        }
    }

    private void OcultarTodosLosDados()
    {
        if (dadosJugadores == null) return;
        foreach (GameObject dado in dadosJugadores)
            if (dado != null) dado.SetActive(false);
    }
}