using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CambiarEscenaMult : MonoBehaviour
{
    public string nombreEscena;

    public void CambiarEscena(string nombreEscena)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(nombreEscena);
    }
}
