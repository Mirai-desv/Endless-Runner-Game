using UnityEngine;

public class DicePickup : MonoBehaviour
{
    [Range(1, 6)]
    public int luckyNumber = 4;

    public int normalReward = 25;

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnergySystem energy = other.GetComponentInParent<EnergySystem>();

        if (energy == null) return;
        int result = Random.Range(1, 7);
        Debug.Log("Dice rolled: " + result);

        if (result == luckyNumber)
        {
            Debug.Log("Lucky Dice!");
            energy.ActivateLuckyDice();
        }
        else
        {
            energy.AddEnergy(normalReward);
        }
        Destroy(gameObject);
    }
}
