
using UnityEngine;

public class PlayerAnimationBridge : MonoBehaviour
{
    public PlayerController controller;
    public PlayerHitReceiver hitReceiver;
    public Animator animator;

    private float nextDebugTime;

    void Awake()
    {
        if (controller == null)
            controller = GetComponent<PlayerController>();

        if (hitReceiver == null)
            hitReceiver = GetComponent<PlayerHitReceiver>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        if (controller != null)
        {
            controller.OnJumpPerformed += HandleJump;
            controller.OnDropPerformed += HandleDrop;
        }
    }

    void Start()
    {
        // Đồng bộ reference nếu được gán muộn.
        if (controller == null)
        {
            controller = GetComponent<PlayerController>();

            if (controller != null)
            {
                controller.OnJumpPerformed += HandleJump;
                controller.OnDropPerformed += HandleDrop;
            }
        }
    }

    void OnDisable()
    {
        if (controller != null)
        {
            controller.OnJumpPerformed -= HandleJump;
            controller.OnDropPerformed -= HandleDrop;
        }
    }

    private void HandleJump()
    {
        if (animator == null) return;

        animator.ResetTrigger("Down");
        animator.SetTrigger("Jump");
    }

    private void HandleDrop()
    {
        if (animator == null) return;

        animator.ResetTrigger("Jump");
        animator.SetTrigger("Down");
    }

    void Update()
    {
        if (controller == null || animator == null)
            return;

        if (hitReceiver != null && hitReceiver.IsDead)
            return;

        bool grounded = controller.IsGrounded;

        animator.SetBool("IsGrounded", grounded);

        if (Time.time >= nextDebugTime)
        {
            nextDebugTime = Time.time + 0.5f;

            Debug.Log(
                $"[ANIMATION] " +
                $"Grounded={grounded} | " +
                $"State={GetCurrentState()}"
            );
        }
    }

    private string GetCurrentState()
    {
        AnimatorStateInfo state =
            animator.GetCurrentAnimatorStateInfo(0);

        if (state.IsName("Witch_Run")) return "Run";
        if (state.IsName("Witch_Jump")) return "Jump";
        if (state.IsName("Witch_Down")) return "Down";
        if (state.IsName("Witch_Shoot")) return "Shoot";
        if (state.IsName("Witch_ShootSpecial")) return "ShootSpecial";
        if (state.IsName("Witch_Die")) return "Die";

        return "Unknown";
    }
}
