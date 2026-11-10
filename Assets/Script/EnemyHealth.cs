
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [Min(1)] public int maxHealth = 1;

    private int currentHealth;
    private bool isDead;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        currentHealth -= damage;

        Debug.Log($"{name} HP: {Mathf.Max(0, currentHealth)}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Sau này có thể thêm hiệu ứng chết, âm thanh, điểm.
        Destroy(gameObject);
    }
}
