using UnityEngine;

public class EnemyContact : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckPlayer(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckPlayer(other);
    }

    private void CheckPlayer(Collider2D other)
    {
        PlayerHitReceiver player =
            other.GetComponentInParent<PlayerHitReceiver>();

        if (player != null)
        {
            player.Hit();
        }
    }
}