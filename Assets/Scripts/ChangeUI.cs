using UnityEngine;

// Muestra una de las 6 imagenes de vida segun la vida actual (la llama PlayerHealth del jugador local)
public class ChangeUI : MonoBehaviour
{
    public GameObject imagen1;
    public GameObject imagen2;
    public GameObject imagen3;
    public GameObject imagen4;
    public GameObject imagen5;
    public GameObject imagen6;

    int imagenActual = -1;

    public void CambiarImagen(int vida)
    {
        int indice;
        if (vida > 80) indice = 1;
        else if (vida > 60) indice = 2;
        else if (vida > 40) indice = 3;
        else if (vida > 20) indice = 4;
        else if (vida > 0) indice = 5;
        else indice = 6;

        if (indice == imagenActual) return; // sin cambios: evita activar/desactivar cada frame (la curacion llama muchas veces)
        imagenActual = indice;

        Activar(imagen1, indice == 1);
        Activar(imagen2, indice == 2);
        Activar(imagen3, indice == 3);
        Activar(imagen4, indice == 4);
        Activar(imagen5, indice == 5);
        Activar(imagen6, indice == 6);
    }

    static void Activar(GameObject go, bool activo)
    {
        if (go != null) go.SetActive(activo);
    }
}
