
using UnityEngine;

public class DifficultySpeed : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;

    [Header("Speed Settings")]
    [SerializeField] private float startSpeed = 10f;
    [SerializeField] private float maxSpeed = 20f;

    [Header("Difficulty")]
    [SerializeField] private float distancePerIncrease = 10f;
    [SerializeField] private float speedIncrease = 0.05f;

    [Header("Smooth")]
    [SerializeField] private float acceleration = 1f;

    private float startX;
    private float currentSpeed;

    private void Start()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerController>();

        if (player == null)
        {
            enabled = false;
            return;
        }

        startX = player.transform.position.x;
        currentSpeed = startSpeed;
        player.RunSpeed = currentSpeed;
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        if (GameManager.Instance != null &&
            GameManager.Instance.IsGameOver)
            return;

        if (GameFlow.Instance != null &&
            GameFlow.Instance.State != GameFlowState.Playing)
            return;

        float distance = Mathf.Max(
            0f,
            player.transform.position.x - startX
        );

        float targetSpeed = startSpeed +
            (distance / distancePerIncrease) * speedIncrease;

        targetSpeed = Mathf.Clamp(
            targetSpeed,
            startSpeed,
            maxSpeed
        );

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            acceleration * Time.fixedDeltaTime
        );

        player.RunSpeed = currentSpeed;
    }
}
