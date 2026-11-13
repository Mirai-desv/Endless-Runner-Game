
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartup : MonoBehaviour
{
    private const string TutorialKey = "TutorialCompleted";

    // Gắn vào nút Play ở Home
    public void PlayGame()
    {
        Time.timeScale = 1f;

        bool completed =
            PlayerPrefs.GetInt(TutorialKey, 0) == 1;

        if (completed)
        {
            SceneManager.LoadScene("Game");
        }
        else
        {
            SceneManager.LoadScene("Tutorial");
        }
    }

    // Gọi sau khi hoàn thành Tutorial
    public static void CompleteTutorial()
    {
        PlayerPrefs.SetInt(TutorialKey, 1);
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
    }

    // Chỉ dùng khi TEST Tutorial từ đầu
    [ContextMenu("Reset Tutorial Progress")]
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(TutorialKey);
        PlayerPrefs.Save();

        Debug.Log("Tutorial progress reset!");
    }
}
