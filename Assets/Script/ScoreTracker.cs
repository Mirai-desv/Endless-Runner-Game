
using UnityEngine;
using TMPro;

public class ScoreTracker : MonoBehaviour
{
    public Transform player;
    public TextMeshProUGUI scoreText;

    private float startX;
    private float maxDistance;

    public int Score =>
        Mathf.FloorToInt(maxDistance);

    void Start()
    {
        startX = player.position.x;
    }

    void Update()
    {
        float distance = player.position.x - startX;

        maxDistance = Mathf.Max(
            maxDistance,
            distance
        );

        if (scoreText != null)
        {
            scoreText.text = "points: " + Score;
        }
    }
}
