using UnityEngine;

public class MoonPickup : MonoBehaviour
{
    public int energyAmount = 10;

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnergySystem energy = other.GetComponentInParent<EnergySystem>();

        if (energy == null) return;

        energy.AddEnergy(energyAmount);
        Destroy(gameObject);
    }
}
