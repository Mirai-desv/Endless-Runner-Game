
using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    [Header("Fall Death")]
    public float deathY = -5f;

    private bool isDead = false;

    void Update()
    {
        if (isDead) return;

        // Rơi khỏi map hoặc xuống hố
        if (transform.position.y < deathY)
        {
            Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle"))
        {
            Die();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log("GAME OVER!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }
}
