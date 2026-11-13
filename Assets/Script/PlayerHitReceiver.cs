using UnityEngine;
using UnityEngine.Events;

public class PlayerHitReceiver : MonoBehaviour
{
    [Header("References")]
    public PlayerController controller;
    public Animator animator;

    [Header("Events")]
    public UnityEvent onPlayerDead;

    private bool isDead;

    public bool IsDead => isDead;

    void Awake()
    {
        if (controller == null)
            controller = GetComponent<PlayerController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void Hit()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log("PLAYER HIT - GAME OVER");

        // Dừng chạy ngay lập tức.
        if (controller != null)
            controller.StopRunning();

        // Phát animation chết.
        if (animator != null)
            animator.SetTrigger("Die");

        // Thông báo hệ thống Game Over.
        onPlayerDead?.Invoke();
    }
}