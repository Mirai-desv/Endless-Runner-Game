
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
    public event Action OnSpecialActivated;

    private void Start()
    {
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);

        if (currentEnergy <= 0)
            specialReady = false;

        UpdateUI();
    }

    public void AddEnergy(int amount)
    {
        if (amount <= 0) return;

        currentEnergy = Mathf.Clamp(
            currentEnergy + amount, 0, maxEnergy
        );

        UpdateUI();
    }

    public bool UseEnergy(int amount)
    {
        if (amount <= 0 || currentEnergy < amount)
            return false;

        currentEnergy -= amount;

        // Kết thúc Special Mode khi không đủ
        // năng lượng để bắn thêm phát tiếp theo.
        if (currentEnergy < shootCost)
        {
            specialReady = false;
        }

        UpdateUI();
        return true;
    }

    public void ActivateLuckyDice()
    {
        // Hồi đầy năng lượng
        currentEnergy = maxEnergy;

        // Kích hoạt Special Mode
        specialReady = true;

        UpdateUI();

        OnSpecialActivated?.Invoke();
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
