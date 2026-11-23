using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UI;

public class TimerScript : MonoBehaviour
{
    public float timer = 60f;
    public Text Contador;
    public void Update()
    {
        timer -= Time.deltaTime;
        Contador.text = "Time: " + timer.ToString("F0");
        if (timer <= 0)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }
}
