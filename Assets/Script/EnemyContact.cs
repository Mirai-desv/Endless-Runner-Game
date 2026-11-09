
using UnityEngine;
using UnityEngine.Events;

public class EnemyContact : MonoBehaviour
{
    public UnityEvent onPlayerHit;

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckPlayer(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckPlayer(collision.collider);
    }

    void CheckPlayer(Collider2D other)
    {
        if (other.GetComponentInParent<EnergySystem>() == null)
            return;

        onPlayerHit.Invoke();
    }
}
