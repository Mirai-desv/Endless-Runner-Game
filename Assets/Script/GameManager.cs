
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Gameplay")]
    public PlayerController player;
    public ScoreTracker scoreTracker;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;

    [Header("Gameplay UI")]
    public GameObject scoreText;
    public GameObject energyBar;
    public GameObject upButton;
    public GameObject downButton;
    public GameObject skillButton;
    public GameObject settingButton;
    public GameObject settingPanel;

    private bool isGameOver = false;

    public bool IsGameOver => isGameOver;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        gameOverPanel.SetActive(false);
    }

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;

        // Dừng nhân vật
        if (player != null)
            player.StopRunning();

        // Lấy điểm cuối cùng
        int score = scoreTracker != null
            ? scoreTracker.Score
            : 0;

        // Hiển thị điểm
        if (finalScoreText != null)
            finalScoreText.text = score.ToString();

        // Ẩn toàn bộ UI gameplay
        HideGameplayUI();

        // Hiện màn hình Game Over
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // Dừng game
        Time.timeScale = 0f;

        Debug.Log("Final Score: " + score);
    }

    private void HideGameplayUI()
    {
        SetVisible(scoreText, false);
        SetVisible(energyBar, false);
        SetVisible(upButton, false);
        SetVisible(downButton, false);
        SetVisible(skillButton, false);
        SetVisible(settingButton, false);
        SetVisible(settingPanel, false);
    }

    private void SetVisible(
        GameObject obj, bool visible)
    {
        if (obj != null)
            obj.SetActive(visible);
    }

    public void Restart()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void GoHome()
    {
        Time.timeScale = 1f;

        // Quay về màn Tap to Play
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}
