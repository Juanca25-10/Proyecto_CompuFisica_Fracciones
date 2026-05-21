using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject panelMenuPausa;

    public void AlternarMenu()
    {
        if (panelMenuPausa == null) return;

        bool estaActivo = !panelMenuPausa.activeSelf;
        panelMenuPausa.SetActive(estaActivo);

        // Congela el juego si el menú está abierto
        Time.timeScale = estaActivo ? 0f : 1f;
    }
}