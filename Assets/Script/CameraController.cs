
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Camera Settings")]
    public float offsetX = 3f;
    public float smoothSpeed = 5f;
    public float fixedY;

    private float fixedZ;

    void Start()
    {
        fixedZ = transform.position.z;

        // Đặt camera đúng vị trí ngay khi bắt đầu
        if (player != null)
        {
            transform.position = new Vector3(
                player.position.x + offsetX,
                fixedY,
                fixedZ
            );
        }
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
            Mathf.Clamp01(smoothSpeed * Time.deltaTime)
        );
    }
}
