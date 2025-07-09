using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public float gameTime = 60f;
    private float currentTime;
    public TextMeshProUGUI timerText;
    public TrackHealth bossTrack;
    public GameObject gameOverUI;
    public GameObject gameClearUI;

    private bool isGameOver = false;

    void Start()
    {
        currentTime = gameTime;
    }

    void Update()
    {
        if (isGameOver) return;

        currentTime -= Time.deltaTime;
        timerText.text = $" {Mathf.Ceil(currentTime)}";

        if (currentTime <= 0)
        {
            GameOver();
        }

        if (bossTrack != null && bossTrack.IsDead())
        {
            GameClear();
        }
    }

    public void AddBonusTime(float amount)
    {
        currentTime += amount;
        Debug.Log($" 보너스 시간 +{amount}초");
    }

    void GameOver()
    {
        isGameOver = true;
        gameOverUI.SetActive(true);
        Time.timeScale = 0;
    }

    void GameClear()
    {
        isGameOver = true;
        gameClearUI.SetActive(true);
        Time.timeScale = 0;
    }
}