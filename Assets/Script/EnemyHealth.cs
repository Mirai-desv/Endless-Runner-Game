using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [Min(1)] public int maxHealth = 1;

    // Thông báo khi một con quái bị tiêu diệt.
    public static event Action<EnemyHealth> OnAnyEnemyDied;

    private int currentHealth;
    private bool isDead;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth -= damage;

        Debug.Log(
            $"{name} HP: " +
            $"{Mathf.Max(0, currentHealth)}/{maxHealth}"
        );

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        // Thông báo TRƯỚC khi Destroy.
        OnAnyEnemyDied?.Invoke(this);

        // Hiệu ứng khói (chỉ chạy nếu object có gắn DeathSmoke).
        DeathSmoke smoke = GetComponent<DeathSmoke>();
        if (smoke != null) smoke.Play();

        Destroy(gameObject);
    }
}