using UnityEngine;
using UnityEngine.UI;

public class AudioVolumeSliders : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Awake()
    {
        if (musicSlider != null)
        {
            musicSlider.minValue = 0f; musicSlider.maxValue = 1f;
            musicSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetMusicVolume(v));
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f; sfxSlider.maxValue = 1f;
            sfxSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSfxVolume(v));
        }
    }

    // Mỗi lần mở panel: kéo slider về đúng giá trị đang lưu
    private void OnEnable()
    {
        AudioManager am = AudioManager.Instance;
        if (am == null) return;

        if (musicSlider != null) musicSlider.SetValueWithoutNotify(am.MusicVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(am.SfxVolume);
    }
}