
using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    public float speed = 12f;
    public float lifetime = 3f;
    public bool isSpecial = false;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(
            Vector3.right * speed * Time.deltaTime,
            Space.World
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Không đụng vào Player
        if (other.GetComponentInParent<EnergySystem>() != null)
            return;

        // Bỏ qua các vật phẩm trigger
        if (other.GetComponentInParent<MoonPickup>() != null ||
            other.GetComponentInParent<DicePickup>() != null)
            return;

        DestructibleObstacle target =
            other.GetComponentInParent<DestructibleObstacle>();

        if (target != null)
        {
            target.DestroyObstacle();

            // Đạn thường biến mất sau khi trúng
            // Đạn đặc biệt tiếp tục bay xuyên qua
            if (!isSpecial)
                Destroy(gameObject);
        }
        else if (!other.isTrigger)
        {
            // Va vào địa hình không phá được
            Destroy(gameObject);
        }
    }
}
