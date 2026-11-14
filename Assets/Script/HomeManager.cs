using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeManager : MonoBehaviour
{
    [Header("Editor Testing")]
    [SerializeField]
    private bool alwaysShowTutorialInEditor = false;

    public void PlayGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        if (alwaysShowTutorialInEditor)
        {
            Debug.Log("[Home] Editor Test -> Tutorial");
            SceneManager.LoadScene("Tutorial");
            return;
        }
#endif

        bool tutorialCompleted =
            PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;

        if (tutorialCompleted)
        {
            Debug.Log("[Home] Tutorial completed -> Game");
            SceneManager.LoadScene("Game");
        }
        else
        {
            Debug.Log("[Home] First time -> Tutorial");
            SceneManager.LoadScene("Tutorial");
        }
    }

    [ContextMenu("Reset Tutorial")]
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey("TutorialCompleted");
        PlayerPrefs.Save();

        Debug.Log("Tutorial progress reset!");
    }
}