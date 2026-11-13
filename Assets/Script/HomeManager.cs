
using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeManager : MonoBehaviour
{
    public void PlayGame()
    {
        Time.timeScale = 1f;

        bool completed =
            PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;

        if (completed)
        {
            // Đã hoàn thành Tutorial
            SceneManager.LoadScene("Game");
        }
        else
        {
            // Lần đầu chơi
            SceneManager.LoadScene("Tutorial");
        }
    }

    // Dùng để test lại Tutorial trong Editor.
    [ContextMenu("Reset Tutorial")]
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey("TutorialCompleted");
        PlayerPrefs.Save();

        Debug.Log("Tutorial progress reset!");
    }
}
