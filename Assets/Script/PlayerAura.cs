
using UnityEngine;

public class PlayerAura : MonoBehaviour
{
    public Animator auraAnimator;
    public SpriteRenderer auraRenderer;

    void Awake()
    {
        if (auraAnimator == null)
            auraAnimator = GetComponent<Animator>();

        if (auraRenderer == null)
            auraRenderer = GetComponent<SpriteRenderer>();

        auraRenderer.enabled = false;
    }

    public void PlayAura()
    {
        auraRenderer.enabled = true;
        auraAnimator.Play("Witch_Aura", 0, 0f);
    }

    public void StopAura()
    {
        auraRenderer.enabled = false;
    }
}
