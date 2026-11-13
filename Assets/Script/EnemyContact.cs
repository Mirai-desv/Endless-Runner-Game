
using UnityEngine;

public class EnemyContact : MonoBehaviour
{
    private TutorialManager tutorialManager;
    private EnemyHealth enemyHealth;

    private void Awake()
    {
        enemyHealth = GetComponentInParent<EnemyHealth>();
    }

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

        if (player == null)
            return;

        if (tutorialManager == null)
        {
            tutorialManager =
                FindAnyObjectByType<TutorialManager>();
        }

        // Trong bước tập bắn:
        // quái mục tiêu không giết Player.
        if (tutorialManager != null &&
            tutorialManager.ShouldIgnoreEnemyContact(enemyHealth))
        {
            return;
        }

        player.Hit();
    }
}
