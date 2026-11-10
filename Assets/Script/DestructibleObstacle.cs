
using UnityEngine;

public class DestructibleObstacle : MonoBehaviour
{
    [Min(1)] public int maxHealth = 1;

    private int currentHealth;
    private bool destroyed;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (destroyed || damage <= 0) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
            DestroyObstacle();
    }

    public void DestroyObstacle()
    {
        if (destroyed) return;

        destroyed = true;
        Destroy(gameObject);
    }
}
