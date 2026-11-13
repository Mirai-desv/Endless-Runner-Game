/*
using UnityEngine;
using UnityEngine.UI;

/// Gắn lên SkillButton. Hiển thị trạng thái nút bắn:
///  - Đủ năng lượng: sáng bình thường
///  - Thiếu năng lượng: tối đi
///  - Đạn đặc biệt sẵn sàng: màu vàng + nhấp nháy phóng to
///  - Bắn: nút "nhún" xuống
///  - Bấm khi không bắn được: nháy đỏ
/// Việc bắn vẫn do Button.OnClick -> PlayerInputBridge.RequestSkill đảm nhiệm.
[RequireComponent(typeof(Button))]
public class SkillButtonUI : MonoBehaviour
{
    [Header("Tham chiếu (tự tìm nếu để trống)")]
    [SerializeField] private EnergySystem energy;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private Graphic icon;

    [Header("Màu")]
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
    [SerializeField] private Color specialColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color rejectedColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Hiệu ứng")]
    [SerializeField] private float pulseSpeed = 6f;
    [SerializeField] private float pulseAmount = 0.08f;
    [SerializeField] private float punchScale = 0.85f;
    [SerializeField] private float punchDuration = 0.12f;
    [SerializeField] private float rejectDuration = 0.2f;

    private Vector3 baseScale;
    private float punchTimer;
    private float rejectTimer;
    private bool hasEnoughEnergy;
    private bool specialReady;

    private void Awake()
    {
        baseScale = transform.localScale;

        if (icon == null)
            icon = GetComponent<Button>().targetGraphic;

        if (energy == null)
            energy = FindFirstObjectByType<EnergySystem>();

        if (combat == null)
            combat = FindFirstObjectByType<PlayerCombat>();
    }

    private void OnEnable()
    {
        if (energy != null) energy.OnEnergyChanged += Refresh;

        if (combat != null)
        {
            combat.OnShot += HandleShot;
            combat.OnShootRejected += HandleRejected;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (energy != null) energy.OnEnergyChanged -= Refresh;

        if (combat != null)
        {
            combat.OnShot -= HandleShot;
            combat.OnShootRejected -= HandleRejected;
        }

        transform.localScale = baseScale;
    }

    private void Refresh()
    {
        if (energy == null) return;

        hasEnoughEnergy = energy.currentEnergy >= energy.shootCost;
        specialReady = energy.specialReady && hasEnoughEnergy;
    }

    private void HandleShot(bool isSpecial) => punchTimer = punchDuration;
    private void HandleRejected() => rejectTimer = rejectDuration;

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        punchTimer = Mathf.Max(0f, punchTimer - dt);
        rejectTimer = Mathf.Max(0f, rejectTimer - dt);

        // Scale: nhấp nháy khi special + nhún khi bắn
        float scale = 1f;

        if (specialReady)
            scale += Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;

        if (punchTimer > 0f)
            scale *= Mathf.Lerp(1f, punchScale, Mathf.Sin(punchTimer / punchDuration * Mathf.PI));

        transform.localScale = baseScale * scale;

        // Màu
        if (icon == null) return;

        Color target = specialReady ? specialColor
                     : hasEnoughEnergy ? readyColor
                     : lockedColor;

        if (rejectTimer > 0f)
            target = Color.Lerp(target, rejectedColor, rejectTimer / rejectDuration);

        icon.color = target;
    }
}
*/