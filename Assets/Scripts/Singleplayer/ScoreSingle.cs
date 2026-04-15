using UnityEngine;

public class ScoreSingle : MonoBehaviour
{
    public int score = 0;
    // Update is called once per frame
    public void AddScore()
    {
        score += 10;
    }
    public int GetScore()
    {
        return score;
    }
    public void downScore()
    {
        score -= 30;
    }
}
