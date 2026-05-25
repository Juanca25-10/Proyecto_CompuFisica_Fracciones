using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    public static MenuPrincipal Instancia; // <--- SINGLETON AUTOMÁTICO

    [Header("Secuencia de Inicio")]
    public float tiempoTotalLogos = 10f;
    public GameObject[] imagenesIntro;

    [Header("Paneles de la UI")]
    public CanvasGroup panelFade;
    public GameObject panelDetectando;
    public GameObject panelMenuOpciones;

    [Header("Efecto de Agitar")]
    public Transform camaraPrincipal;
    private Vector3 posicionOriginalCamara;

    private bool enMenuPrincipal = false;

    private void Awake()
    {
        Instancia = this; // Se auto-asigna al nacer
    }

    void Start()
    {
        panelMenuOpciones.SetActive(false);
        panelDetectando.SetActive(true);
        panelFade.gameObject.SetActive(true);
        panelFade.alpha = 1f;

        if (imagenesIntro != null)
            foreach (GameObject logo in imagenesIntro)
                if (logo != null) logo.SetActive(false);

        if (camaraPrincipal != null) posicionOriginalCamara = camaraPrincipal.localPosition;

        StartCoroutine(SecuenciaDeInicio());
    }

    private IEnumerator SecuenciaDeInicio()
    {
        if (imagenesIntro != null && imagenesIntro.Length > 0)
        {
            float tiempoPorLogo = tiempoTotalLogos / imagenesIntro.Length;
            for (int i = 0; i < imagenesIntro.Length; i++)
            {
                if (imagenesIntro[i] != null)
                {
                    imagenesIntro[i].SetActive(true);
                    yield return new WaitForSeconds(tiempoPorLogo);
                    imagenesIntro[i].SetActive(false);
                }
            }
        }
        else { yield return new WaitForSeconds(1f); }

        float t = 0;
        while (t < 1f) { t += Time.deltaTime * 0.8f; panelFade.alpha = Mathf.Lerp(1f, 0f, t); yield return null; }
        panelFade.gameObject.SetActive(false);

        Debug.Log("Buscando dispositivo...");
        yield return new WaitForSeconds(3f); // Espera del dispositivo

        Debug.Log("¡Dispositivo encontrado!");
        panelDetectando.SetActive(false);
        panelMenuOpciones.SetActive(true);
        enMenuPrincipal = true;
    }

    void Update()
    {
        if (!enMenuPrincipal) return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.zKey.wasPressedThisFrame) AccionEmpezar();
            if (Keyboard.current.xKey.wasPressedThisFrame) AccionAjustes();
            if (Keyboard.current.cKey.wasPressedThisFrame) AccionComoJugar();
            if (Keyboard.current.vKey.wasPressedThisFrame) AccionSalir();
            if (Keyboard.current.spaceKey.wasPressedThisFrame) EfectoAgitarHardware();
        }
    }

    // Estas funciones ahora son llamadas directamente por el Arduino
    public void AccionEmpezar() { SceneManager.LoadScene("escenaPruebaJulian"); } // Cambia por tu escena
    public void AccionAjustes() { Debug.Log("Abriendo AJUSTES..."); }
    public void AccionComoJugar() { Debug.Log("Abriendo COMO JUGAR..."); }
    public void AccionSalir() { Application.Quit(); }

    public void EfectoAgitarHardware()
    {
        if (enMenuPrincipal) StartCoroutine(TemblorDeCamara());
    }

    private IEnumerator TemblorDeCamara()
    {
        float duracion = 0.4f; float magnitud = 0.3f; float tiempo = 0f;
        while (tiempo < duracion)
        {
            if (camaraPrincipal != null)
            {
                float x = posicionOriginalCamara.x + Random.Range(-1f, 1f) * magnitud;
                float y = posicionOriginalCamara.y + Random.Range(-1f, 1f) * magnitud;
                camaraPrincipal.localPosition = new Vector3(x, y, posicionOriginalCamara.z);
            }
            tiempo += Time.deltaTime; yield return null;
        }
        if (camaraPrincipal != null) camaraPrincipal.localPosition = posicionOriginalCamara;
    }
}