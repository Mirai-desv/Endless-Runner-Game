
using UnityEngine;
using System;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public EnergySystem energy;
    public Animator animator;
    public PlayerHitReceiver hitReceiver;

    [Header("Projectile")]
    public GameObject normalBullet;
    public GameObject specialBullet;
    public Transform firePoint;

    public event Action OnNormalShot;
    public event Action OnSpecialShot;

    void Awake()
    {
        if (energy == null)
            energy = GetComponent<EnergySystem>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (hitReceiver == null)
            hitReceiver = GetComponent<PlayerHitReceiver>();
    }

    void Update()
    {
        if (GameFlow.InputLocked) return;

        if (Input.GetKeyDown(KeyCode.Space))
            Shoot();
    }


    public void Shoot()
    {
        if (GameFlow.InputLocked)
            return;

        if (hitReceiver != null && hitReceiver.IsDead)
            return;

        if (energy == null || firePoint == null)
            return;

        if (energy.currentEnergy < energy.shootCost)
            return;

        // Xác định chế độ bắn trước khi trừ năng lượng
        bool isSpecial = energy.specialReady;

        GameObject prefab =
            isSpecial ? specialBullet : normalBullet;

        if (prefab == null)
            return;

        // EnergySystem tự quản lý việc kết thúc Special Mode
        if (!energy.UseEnergy(energy.shootCost))
            return;

        if (animator != null)
        {
            animator.SetTrigger(
                isSpecial ? "ShootSpecial" : "Shoot"
            );
        }

        Instantiate(
            prefab,
            firePoint.position,
            Quaternion.identity
        );

        if (isSpecial)
        {
            OnSpecialShot?.Invoke();
        }
        else
        {
            OnNormalShot?.Invoke();
        }
    }

}
