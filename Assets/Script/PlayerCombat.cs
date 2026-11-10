
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public EnergySystem energy;

    [Header("Projectile")]
    public GameObject normalBullet;
    public GameObject specialBullet;
    public Transform firePoint;

    void Awake()
    {
        if (energy == null)
            energy = GetComponent<EnergySystem>();
    }

    void Update()
    {
        if (GameFlow.InputLocked) return;

        if (Input.GetKeyDown(KeyCode.J))
        {
            Shoot();
        }
    }

    public void Shoot()
    {
        if (energy == null || firePoint == null)
            return;

        if (energy.currentEnergy < energy.shootCost)
            return;

        bool isSpecial = energy.specialReady;

        GameObject prefab =
            isSpecial ? specialBullet : normalBullet;

        if (prefab == null) return;

        if (!energy.UseEnergy(energy.shootCost))
            return;

        Instantiate(
            prefab,
            firePoint.position,
            Quaternion.identity
        );

        if (isSpecial)
            energy.specialReady = false;
    }
}
