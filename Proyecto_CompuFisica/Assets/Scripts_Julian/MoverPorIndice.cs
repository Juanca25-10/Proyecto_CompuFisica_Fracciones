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
    public GameObject camaraGlobalTablero;

    [Header("Dados 3D de los Jugadores")]
    public GameObject[] dadosJugadores;

    [Header("Configuración del Dado")]
    public float tiempoGiroDado = 1.5f;
    public float tiempoEsperaResultado = 1f;

    [Tooltip("Rotaciones para las caras 1 a 6.")]
    public Vector3[] rotacionesCaras = new Vector3[6]
    {
        new Vector3(0, 0, 0),
        new Vector3(90, 0, 0),
        new Vector3(0, 90, 0),
        new Vector3(0, -90, 0),
        new Vector3(-90, 0, 0),
        new Vector3(180, 0, 0)
    };

    [Header("Posiciones")]
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
        if (camaraGlobalTablero != null) camaraGlobalTablero.SetActive(false);
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

    // Compatibilidad por si usan teclado
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

    public void CelebrarJugador()
    {
        if (estaProcesandoTurno || juegoTerminado) return;
        StartCoroutine(RutinaSaltoCelebracion());
    }

    private IEnumerator RutinaSaltoCelebracion()
    {
        GameObject ficha = jugadores[turnoActual];
        Vector3 posOriginal = posiciones[casillasActuales[turnoActual]].position;
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            Vector3 posActual = posOriginal;
            posActual.y += Mathf.Sin(t * Mathf.PI) * 0.8f;
            ficha.transform.position = posActual;
            yield return null;
        }
        ficha.transform.position = posOriginal;
    }

    public void AlternarCamaraGlobal()
    {
        if (camaraGlobalTablero == null) return;
        camaraGlobalTablero.SetActive(!camaraGlobalTablero.activeSelf);
    }

    private IEnumerator SecuenciaTurnoCompleta()
    {
        estaProcesandoTurno = true;
        GameObject dadoActual = dadosJugadores[turnoActual];
        if (dadoActual != null) dadoActual.SetActive(true);

        int resultadoDado = Random.Range(1, 7);

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
            yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitForSeconds(tiempoEsperaResultado);
        if (dadoActual != null) dadoActual.SetActive(false);

        int casillaAnterior = casillasActuales[turnoActual];
        int nuevaCasilla = casillaAnterior + resultadoDado;

        if (nuevaCasilla >= posiciones.Length) nuevaCasilla = posiciones.Length - 1;

        yield return StartCoroutine(MoverFichaPasoAPaso(turnoActual, casillaAnterior, nuevaCasilla));

        if (casillasActuales[turnoActual] >= posiciones.Length - 1)
        {
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