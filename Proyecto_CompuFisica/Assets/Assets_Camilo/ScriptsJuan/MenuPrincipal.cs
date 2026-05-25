using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [Header("Secuencia de Inicio (Logos)")]
    [Tooltip("Tiempo total en segundos que durará la pantalla negra mostrando logos.")]
    public float tiempoTotalLogos = 10f;
    [Tooltip("Arrastra aquí las imágenes (UI) de tus logos.")]
    public GameObject[] imagenesIntro;

    [Header("Paneles de la UI")]
    public CanvasGroup panelFade;
    public GameObject panelDetectando;
    public GameObject panelMenuOpciones;

    [Header("Efecto de Agitar")]
    public Transform camaraPrincipal;
    private Vector3 posicionOriginalCamara;

    // Variables de control
    private bool enMenuPrincipal = false;

    void Start()
    {
        // Estado inicial
        panelMenuOpciones.SetActive(false);
        panelDetectando.SetActive(true); // Se queda esperando detrás de la pantalla negra
        panelFade.gameObject.SetActive(true);
        panelFade.alpha = 1f; // Pantalla totalmente negra

        // Asegurarnos de que todos los logos empiecen apagados
        if (imagenesIntro != null)
        {
            foreach (GameObject logo in imagenesIntro)
            {
                if (logo != null) logo.SetActive(false);
            }
        }

        if (camaraPrincipal != null)
            posicionOriginalCamara = camaraPrincipal.localPosition;

        StartCoroutine(SecuenciaDeInicio());
    }

    private IEnumerator SecuenciaDeInicio()
    {
        // ==============================================================
        // FASE 1: MOSTRAR LOGOS (ESTILO CINE)
        // ==============================================================
        if (imagenesIntro != null && imagenesIntro.Length > 0)
        {
            // Calculamos cuánto tiempo le toca a cada logo
            float tiempoPorLogo = tiempoTotalLogos / imagenesIntro.Length;

            for (int i = 0; i < imagenesIntro.Length; i++)
            {
                if (imagenesIntro[i] != null)
                {
                    imagenesIntro[i].SetActive(true);           // Encendemos el logo
                    yield return new WaitForSeconds(tiempoPorLogo); // Esperamos su turno
                    imagenesIntro[i].SetActive(false);          // Lo apagamos
                }
            }
        }
        else
        {
            // Si dejaste la lista vacía, igual esperamos un segundito en negro por estética
            yield return new WaitForSeconds(1f);
        }

        // ==============================================================
        // FASE 2: FADE (Desaparece la pantalla negra)
        // ==============================================================
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 0.8f;
            panelFade.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }
        panelFade.gameObject.SetActive(false); // Apagamos el panel negro

        // ==============================================================
        // FASE 3: ESPERAR DISPOSITIVO (ARDUINO)
        // ==============================================================
        Debug.Log("Buscando dispositivo...");
        yield return new WaitForSeconds(5f); // Simulador de los 5 segundos

        Debug.Log("¡Dispositivo encontrado!");
        panelDetectando.SetActive(false);
        panelMenuOpciones.SetActive(true);
        enMenuPrincipal = true;
    }

    void Update()
    {
        if (!enMenuPrincipal) return;

        // MODO PRUEBA (NUEVO INPUT SYSTEM)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.zKey.wasPressedThisFrame) AccionEmpezar();
            if (Keyboard.current.xKey.wasPressedThisFrame) AccionAjustes();
            if (Keyboard.current.cKey.wasPressedThisFrame) AccionComoJugar();
            if (Keyboard.current.vKey.wasPressedThisFrame) AccionSalir();

            if (Keyboard.current.spaceKey.wasPressedThisFrame) EfectoAgitarHardware();
        }
    }

    public void AccionEmpezar() { SceneManager.LoadScene("escenaPruebaJulian"); }
    public void AccionAjustes() { Debug.Log("Abriendo AJUSTES..."); }
    public void AccionComoJugar() { Debug.Log("Abriendo COMO JUGAR..."); }
    public void AccionSalir() { Application.Quit(); }

    public void EfectoAgitarHardware()
    {
        if (enMenuPrincipal) StartCoroutine(TemblorDeCamara());
    }

    private IEnumerator TemblorDeCamara()
    {
        float duracion = 0.4f;
        float magnitud = 0.3f;
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            if (camaraPrincipal != null)
            {
                float x = posicionOriginalCamara.x + Random.Range(-1f, 1f) * magnitud;
                float y = posicionOriginalCamara.y + Random.Range(-1f, 1f) * magnitud;
                camaraPrincipal.localPosition = new Vector3(x, y, posicionOriginalCamara.z);
            }
            tiempo += Time.deltaTime;
            yield return null;
        }

        if (camaraPrincipal != null) camaraPrincipal.localPosition = posicionOriginalCamara;
    }
}