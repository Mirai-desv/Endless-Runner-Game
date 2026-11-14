
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

        isGameOver = false;
        Time.timeScale = 1f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        Debug.Log(
            $"[GameManager] Initialized | " +
            $"ID={GetEntityId()} | " +
            $"Scene={gameObject.scene.name}"
        );
    }

    void OnDestroy()
    {
        Debug.Log(
            $"[GameManager] Destroyed | ID={GetEntityId()}"
        );

        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        isGameOver = false;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void GameOver()
    {
        Debug.Log("[GameManager] GameOver() CALLED");

        if (isGameOver)
        {
            Debug.LogWarning("[GameManager] Game đã ở trạng thái GameOver!");
            return;
        }

        isGameOver = true;

        if (player != null)
            player.StopRunning();

        int score = scoreTracker != null
            ? scoreTracker.Score
            : 0;

        if (finalScoreText != null)
            finalScoreText.text = score.ToString();

        HideGameplayUI();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            Debug.Log(
                "[GameManager] GameOverPanel activeSelf = " +
                gameOverPanel.activeSelf +
                ", activeInHierarchy = " +
                gameOverPanel.activeInHierarchy
            );
        }
        else
        {
            Debug.LogError("[GameManager] GameOverPanel chưa được gán!");
        }

        Time.timeScale = 0f;

        Debug.Log("[GameManager] Final Score: " + score);
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
        ResetGameOver();

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    public void GoHome()
    {
        ResetGameOver();

        SceneManager.LoadScene("Home");
    }


    private void ResetGameOver()
    {
        isGameOver = false;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        Time.timeScale = 1f;
    }

}
