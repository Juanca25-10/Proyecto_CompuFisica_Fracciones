using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject panelMenuPausa;

    // Esta función abrirá o cerrará el menú cada vez que se llame
    public void AlternarMenu()
    {
        if (panelMenuPausa == null) return;

        // Si está activo lo apaga, si está apagado lo activa
        bool estaActivo = !panelMenuPausa.activeSelf;
        panelMenuPausa.SetActive(estaActivo);

        // Pausa el tiempo del juego si el menú está abierto
        Time.timeScale = estaActivo ? 0f : 1f;
    }
}
