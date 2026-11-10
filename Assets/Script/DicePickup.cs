using UnityEngine;

// Vật phẩm xúc xắc đặt cố định trên đường. Player chạm vào -> mở màn quay xúc xắc (GameFlow).
// Quay trúng số may mắn (chọn ở đầu game) -> đầy thanh năng lượng, không trúng -> + normalReward.
public class DicePickup : MonoBehaviour
{
    [Tooltip("Chỉ dùng khi scene KHÔNG có GameFlow (không có màn chọn số). " +
             "Bình thường số may mắn do người chơi chọn ở đầu game.")]
    [Range(1, 6)]
    public int luckyNumber = 4;

    public int normalReward = 25;

    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        EnergySystem energy = other.GetComponentInParent<EnergySystem>();
        if (energy == null) return;

        GameFlow flow = GameFlow.Instance;
        if (flow != null)
        {
            int lucky = flow.LuckyNumber;
            int reward = normalReward;

            // Callback không giữ tham chiếu tới DicePickup vì object này bị Destroy ngay bên dưới.
            if (!flow.StartDiceRoll(reward, result => ApplyResult(energy, result, lucky, reward)))
                return; // chưa mở được màn quay -> giữ nguyên vật phẩm, lần chạm sau thử lại
        }
        else
        {
            // Không có GameFlow: giữ cách cũ (random tức thì).
            ApplyResult(energy, Random.Range(1, 7), luckyNumber, normalReward);
        }

        triggered = true;
        Destroy(gameObject);
    }

    private static void ApplyResult(EnergySystem energy, int result, int lucky, int reward)
    {
        if (energy == null) return; // scene đã bị load lại

        Debug.Log("Dice rolled: " + result + " (lucky = " + lucky + ")");

        if (result == lucky)
        {
            Debug.Log("Lucky Dice!");
            energy.ActivateLuckyDice();
        }
        else
        {
            energy.AddEnergy(reward);
        }
    }
}
