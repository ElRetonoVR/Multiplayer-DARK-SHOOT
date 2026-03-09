using UnityEngine;

public class sceneChange : MonoBehaviour
{
    public string sceneName;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}