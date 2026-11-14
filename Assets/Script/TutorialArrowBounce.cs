using UnityEngine;

public class TutorialArrowBounce : MonoBehaviour
{
    [Header("Bounce Settings")]
    public float amplitude = 12f;  // Độ cao nhấp nhô
    public float speed = 3f;        // Tốc độ nhấp nhô

    private RectTransform rect;
    private Vector2 startPosition;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        startPosition = rect.anchoredPosition;
    }

    void Update()
    {
        if (rect == null) return;

        float offsetY = Mathf.Sin(Time.unscaledTime * speed) * amplitude;

        rect.anchoredPosition = startPosition
            + new Vector2(0f, offsetY);
    }
}