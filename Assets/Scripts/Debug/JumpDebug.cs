using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class JumpDebug : MonoBehaviour
{
    PlayerController pc;
    Rigidbody2D rb;

    void Awake() { pc = GetComponent<PlayerController>(); rb = GetComponent<Rigidbody2D>(); }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.UpArrow)) return;

        string s = $"[JumpDebug] timeScale={Time.timeScale} simulated={rb.simulated} " +
                   $"vy={rb.linearVelocity.y:0.00} groundMask={pc.groundLayer.value}\n";

        var hits = Physics2D.OverlapCircleAll(pc.groundCheck.position, pc.checkRadius); // mọi layer
        if (hits.Length == 0) s += "GroundCheck KHÔNG chạm collider nào.";
        foreach (var h in hits)
        {
            bool inMask = (pc.groundLayer.value & (1 << h.gameObject.layer)) != 0;
            s += $"- {GetPath(h.transform)} | layer={LayerMask.LayerToName(h.gameObject.layer)} | inMask={inMask}\n";
        }
        Debug.Log(s, this);
    }

    static string GetPath(Transform t) { string p = t.name; while (t.parent) { t = t.parent; p = t.name + "/" + p; } return p; }

    void OnDrawGizmos()
    {
        if (pc == null) pc = GetComponent<PlayerController>();
        if (pc && pc.groundCheck) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(pc.groundCheck.position, pc.checkRadius); }
    }
}