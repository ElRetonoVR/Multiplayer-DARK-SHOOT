using UnityEngine;
using UnityEngine.UI;

public class TimerScript : MonoBehaviour
{
    public float timer = 60f;
    public Text Contador;
    private bool terminado = false;

    public void Update()
    {
        if (terminado) return;

        timer -= Time.deltaTime;
        Contador.text = "Time: " + Mathf.Max(timer, 0f).ToString("F0");

        if (timer <= 0)
        {
            terminado = true; // Carga la escena una sola vez
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }
}
