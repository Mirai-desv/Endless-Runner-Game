
using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeManager : MonoBehaviour
{

    public void PlayGame()
    {
        Time.timeScale = 1f;

        bool tutorialCompleted =
            PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;

        if (tutorialCompleted)
        {
            Debug.Log("[Home] Tutorial completed -> Game");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        }
        else
        {
            Debug.Log("[Home] First time -> Tutorial");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Tutorial");
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
