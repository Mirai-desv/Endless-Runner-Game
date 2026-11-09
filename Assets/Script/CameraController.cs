
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;
    public float offsetX = 3f;
    public float smoothSpeed = 5f;

    private float fixedY;
    private float fixedZ;

    void Start()
    {
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 target = new Vector3(
            player.position.x + offsetX,
            fixedY,
            fixedZ
        );

        transform.position = Vector3.Lerp(
            transform.position,
            target,
            smoothSpeed * Time.deltaTime
        );
    }
}
