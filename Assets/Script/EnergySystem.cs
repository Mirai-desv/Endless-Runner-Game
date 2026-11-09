using System;
using UnityEngine;
using UnityEngine.UI;

public class EnergySystem : MonoBehaviour
{
    [Header("Energy Settings")]
    public int maxEnergy = 100;
    public int currentEnergy = 0;
    public int shootCost = 10;

    [Header("Special Mode")]
    public bool specialReady = false;

    [Header("UI")]
    public Slider energySlider;
    public event Action OnEnergyChanged;
    private void Start()
    {
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        UpdateUI();
    }

    public void AddEnergy(int amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, maxEnergy);
        UpdateUI();
    }

    public bool UseEnergy(int amount)
    {
        if (currentEnergy < amount) return false;
        currentEnergy -= amount;
        UpdateUI();
        return true;
    }

    public void ActivateLuckyDice()
    {
        currentEnergy = maxEnergy;
        specialReady = true;
        UpdateUI();
    }

    public bool ConsumeSpecialShoot()
    {
        if (!specialReady) return false;
        specialReady = false;
        UpdateUI();
        return true;
    }

    private void UpdateUI()
    {
        if (energySlider != null)
        {
            energySlider.minValue = 0;
            energySlider.maxValue = maxEnergy;
            energySlider.value = currentEnergy;
        }
        OnEnergyChanged?.Invoke();
    }

}
