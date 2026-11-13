
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class GameStartup : MonoBehaviour
{
    private const string TutorialKey = "TutorialCompleted";

    private void Awake()
    {
        if (PlayerPrefs.GetInt(TutorialKey, 0) == 1)
        {
            SceneManager.LoadScene("Game");
        }
    }

    public static void CompleteTutorial()
    {
        PlayerPrefs.SetInt(TutorialKey, 1);
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
    }
}
