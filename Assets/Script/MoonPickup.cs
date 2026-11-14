using EndlessRunner.Run.World;
using UnityEngine;

// Moon nằm trong chunk. Chunk được pool tái sử dụng, nên KHÔNG Destroy khi ăn:
// chỉ tắt đi, rồi bật lại khi chunk được lấy ra dùng lại (ChunkView.ResetForReuse).
public class MoonPickup : MonoBehaviour, IChunkResettable
{
    public int energyAmount = 10;

    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        EnergySystem energy = other.GetComponentInParent<EnergySystem>();

        if (energy == null) return;

        collected = true;
        energy.AddEnergy(energyAmount);
        gameObject.SetActive(false);
    }

    // Mỗi lần chunk được sinh lại thì moon xuất hiện lại.
    public void ResetForReuse()
    {
        collected = false;
        gameObject.SetActive(true);
    }
}