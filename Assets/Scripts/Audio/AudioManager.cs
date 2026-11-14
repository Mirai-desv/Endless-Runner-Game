using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// List SFX in game. If you want to take a new sound, please add name in this list
public enum SoundId
{
    NormalShot,   // bắn đạn thường
    SpecialShot,  // bắn đạn siêu cấp
    Jump,         // nhảy
    DiceRoll,     // rung / quay xúc xắc (phát lặp trong lúc quay)
    EnemyAttack,  // quái bắt đầu tấn công (vung / lên nòng)
    EnemyBullet,  // đạn của quái bắn ra
    WallHit,      // đạn bắn trúng tường / vật cản
    Pickup,       // ăn điểm (Moon)
    Die,          // chết
    ButtonClick,  // bấm nút
    MenuShow      // menu / panel xuất hiện
}

[System.Serializable]
public class SoundEntry
{
    [Tooltip("Tên hiển thị (tự điền theo Id)")]
    public string name;
    public SoundId id;

    [Tooltip("Nhiều clip = mỗi lần phát chọn ngẫu nhiên 1 clip")]
    public AudioClip[] clips;

    [Range(0f, 1f)] public float volume = 1f;

    [Tooltip("Mỗi lần phát lấy pitch ngẫu nhiên trong khoảng này. Để 2 số bằng nhau = không đổi.")]
    [Range(0.5f, 2f)] public float pitchMin = 1f;
    [Range(0.5f, 2f)] public float pitchMax = 1f;

    [Tooltip("Khoảng cách tối thiểu (giây) giữa 2 lần phát, tránh chồng âm khi bị gọi dồn dập.")]
    [Min(0f)] public float minInterval = 0.03f;

    [Tooltip("Phát lặp cho tới khi gọi Stop (dùng cho tiếng quay xúc xắc). Chỉ 1 âm lặp chạy cùng lúc.")]
    public bool loop;
}

/// Quản lý TOÀN BỘ âm thanh: nhạc nền + hiệu ứng (SFX), chỉnh âm lượng, tắt tiếng.
///
/// Cách dùng:
///  1. Tạo 1 GameObject rỗng "AudioManager" trong scene đầu tiên, gắn component này.
///  2. Kéo clip vào ô Background Music và từng ô trong danh sách Sounds.
///  3. Từ code gọi:  AudioManager.Play(SoundId.Pickup);
///
/// Tự động nối sẵn (không cần sửa script khác): bắn thường/siêu cấp, nhảy, chết,
/// menu xuất hiện, xúc xắc quay, bấm nút (Button UI + chạm Tap to play / chọn số).
/// Những âm còn lại cần 1 dòng AudioManager.Play(...) ở script tương ứng (xem README đi kèm).
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Nhạc nền")]
    public AudioClip backgroundMusic;
    public bool playMusicOnStart = true;
    [Min(0f)] public float musicFadeTime = 1f;

    [Header("Âm lượng mặc định (người chơi chỉnh sẽ được lưu lại)")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    [Header("Hiệu ứng âm thanh (SFX)")]
    [SerializeField] private SoundEntry[] sounds;
    [Tooltip("Số âm được phát cùng lúc.")]
    [SerializeField, Min(2)] private int voiceCount = 12;

    [Header("Tự động nối vào game")]
    [SerializeField] private bool autoHook = true;
    [Tooltip("Không phát tiếng bấm nút cho nút Nhảy / Xuống / Bắn (các nút này đã có tiếng riêng).")]
    [SerializeField] private bool ignoreGameplayButtons = true;
    [Tooltip("Thêm các nút khác không muốn có tiếng bấm.")]
    [SerializeField] private Button[] ignoredButtons;
    [Tooltip("Hạ nhỏ nhạc nền khi đang quay xúc xắc và khi chết.")]
    [SerializeField] private bool duckMusic = true;
    [Range(0f, 1f)] [SerializeField] private float duckVolume = 0.35f;
    [Tooltip("Sau khi chết, chờ bao lâu (giây) thì phát tiếng hiện màn Game Over.")]
    [SerializeField, Min(0f)] private float gameOverMenuDelay = 0.8f;

    // ------------------------------------------------------------------ Trạng thái
    private const string KeyMaster = "audio_master";
    private const string KeyMusic = "audio_music";
    private const string KeySfx = "audio_sfx";
    private const string KeyMuted = "audio_muted";

    private bool muted;

    private AudioSource musicSource;
    private AudioSource loopSource;
    private AudioSource[] voices;
    private int nextVoice;

    private readonly Dictionary<SoundId, SoundEntry> table = new();
    private float[] lastPlayed;

    private float musicFade;
    private float musicFadeTarget = 1f;
    private float musicFadeSpeed = 1f;
    private bool stopMusicWhenSilent;
    private float duckCurrent = 1f;

    private bool hasLoop;
    private SoundId loopId;
    private float loopBaseVolume;

    // Tự nối
    private PlayerCombat combat;
    private PlayerController controller;
    private PlayerHitReceiver hitReceiver;
    private float nextBindTime;

    private GameFlowState lastState;
    private bool hasLastState;
    private bool settingsWasOpen;
    private bool deathHandled;
    private bool pendingMenuShow;
    private float pendingMenuAt;

    private readonly HashSet<Button> hookedButtons = new();
    private float nextButtonScan;

    // ------------------------------------------------------------------ Âm lượng (gắn được vào Slider)
    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public bool Muted => muted;

    public void SetMasterVolume(float v) { masterVolume = Mathf.Clamp01(v); PlayerPrefs.SetFloat(KeyMaster, masterVolume); }
    public void SetMusicVolume(float v) { musicVolume = Mathf.Clamp01(v); PlayerPrefs.SetFloat(KeyMusic, musicVolume); }
    public void SetSfxVolume(float v) { sfxVolume = Mathf.Clamp01(v); PlayerPrefs.SetFloat(KeySfx, sfxVolume); }
    public void SetMuted(bool value) { muted = value; PlayerPrefs.SetInt(KeyMuted, value ? 1 : 0); }
    public void ToggleMute() => SetMuted(!muted);

    private float SfxGain => muted ? 0f : masterVolume * sfxVolume;
    private float MusicGain => muted ? 0f : masterVolume * musicVolume;

    // ------------------------------------------------------------------ API tĩnh (gọi từ script khác)
    public static void Play(SoundId id, float volumeScale = 1f)
    {
        if (Instance != null) Instance.PlayInternal(id, volumeScale);
    }

    public static void Stop(SoundId id)
    {
        if (Instance != null) Instance.StopInternal(id);
    }

    // Dành cho UnityEvent trong Inspector (Inspector không truyền được enum)
    public void PlayButtonClick() => PlayInternal(SoundId.ButtonClick, 1f);
    public void PlayMenuShow() => PlayInternal(SoundId.MenuShow, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    // ------------------------------------------------------------------ Lifecycle
    private void Reset() => EnsureEntries();
    private void OnValidate() => EnsureEntries();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // scene khác cũng có AudioManager: giữ bản đầu tiên
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        masterVolume = PlayerPrefs.GetFloat(KeyMaster, masterVolume);
        musicVolume = PlayerPrefs.GetFloat(KeyMusic, musicVolume);
        sfxVolume = PlayerPrefs.GetFloat(KeySfx, sfxVolume);
        muted = PlayerPrefs.GetInt(KeyMuted, 0) == 1;

        EnsureEntries();
        table.Clear();
        foreach (SoundEntry e in sounds) table[e.id] = e;

        lastPlayed = new float[System.Enum.GetValues(typeof(SoundId)).Length];
        for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = -999f;

        musicSource = CreateSource("Music", true);
        loopSource = CreateSource("SFX Loop", true);
        voices = new AudioSource[Mathf.Max(2, voiceCount)];
        for (int i = 0; i < voices.Length; i++) voices[i] = CreateSource("SFX " + i, false);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (Instance != this) return;
        if (playMusicOnStart) PlayMusic();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this) return;

        // Scene mới (Retry / Home): đặt lại trạng thái theo dõi
        hasLastState = false;
        settingsWasOpen = false;
        deathHandled = false;
        pendingMenuShow = false;
        hookedButtons.Clear();
        nextBindTime = 0f;
        nextButtonScan = 0f;
        StopInternal(SoundId.DiceRoll);
    }

    private void Update()
    {
        if (Instance != this) return;

        if (autoHook)
        {
            TryBindPlayer();
            TrackFlow();
            TrackSettings();
            TrackGameOver();
            ScanButtons();
        }

        if (pendingMenuShow && Time.unscaledTime >= pendingMenuAt)
        {
            pendingMenuShow = false;
            PlayInternal(SoundId.MenuShow, 1f);
        }

        UpdateMusic(Time.unscaledDeltaTime);
        UpdateLoop();
    }

    // ------------------------------------------------------------------ Nhạc nền
    public void PlayMusic(AudioClip clip = null)
    {
        if (clip == null) clip = backgroundMusic;
        if (clip == null || musicSource == null) return;

        stopMusicWhenSilent = false;
        musicFadeTarget = 1f;
        musicFadeSpeed = 1f / Mathf.Max(0.01f, musicFadeTime);

        // Đang phát đúng bài này rồi (ví dụ sau khi Retry) thì giữ nguyên, không phát lại từ đầu
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicFade = musicFadeTime > 0f ? 0f : 1f;
        musicSource.volume = 0f;
        musicSource.Play();
    }

    public void StopMusic(float fadeOut = 0.5f)
    {
        musicFadeTarget = 0f;
        musicFadeSpeed = 1f / Mathf.Max(0.01f, fadeOut);
        stopMusicWhenSilent = true;
    }

    private void UpdateMusic(float dt)
    {
        musicFade = Mathf.MoveTowards(musicFade, musicFadeTarget, dt * musicFadeSpeed);

        bool ducking = duckMusic &&
            (deathHandled || (hasLastState && lastState == GameFlowState.DiceRoll));
        duckCurrent = Mathf.MoveTowards(duckCurrent, ducking ? duckVolume : 1f, dt * 3f);

        musicSource.volume = MusicGain * musicFade * duckCurrent;

        if (stopMusicWhenSilent && musicFade <= 0.001f)
        {
            musicSource.Stop();
            stopMusicWhenSilent = false;
        }
    }

    // ------------------------------------------------------------------ SFX
    private void PlayInternal(SoundId id, float volumeScale)
    {
        if (!table.TryGetValue(id, out SoundEntry e)) return;

        AudioClip clip = PickClip(e.clips);
        if (clip == null) return;

        // Dùng unscaledTime để vẫn hoạt động khi game đang pause (timeScale = 0)
        float now = Time.unscaledTime;
        int index = (int)id;
        if (now - lastPlayed[index] < e.minInterval) return;
        lastPlayed[index] = now;

        float pitch = UnityEngine.Random.Range(
            Mathf.Min(e.pitchMin, e.pitchMax), Mathf.Max(e.pitchMin, e.pitchMax));
        float baseVolume = e.volume * volumeScale;

        if (e.loop)
        {
            loopSource.clip = clip;
            loopSource.pitch = pitch;
            loopSource.loop = true;
            loopSource.volume = baseVolume * SfxGain;
            loopSource.Play();
            hasLoop = true;
            loopId = id;
            loopBaseVolume = baseVolume;
            return;
        }

        AudioSource src = GetVoice();
        src.clip = clip;
        src.pitch = pitch;
        src.volume = baseVolume * SfxGain;
        src.Play();
    }

    private void StopInternal(SoundId id)
    {
        if (!hasLoop || loopId != id) return;

        loopSource.Stop();
        hasLoop = false;
    }

    private void UpdateLoop()
    {
        if (!hasLoop) return;

        if (!loopSource.isPlaying) { hasLoop = false; return; }
        loopSource.volume = loopBaseVolume * SfxGain;
    }

    // Ưu tiên nguồn đang rảnh; hết rảnh thì cướp nguồn kế tiếp (vòng tròn)
    private AudioSource GetVoice()
    {
        for (int i = 0; i < voices.Length; i++)
        {
            int idx = (nextVoice + i) % voices.Length;
            if (!voices[idx].isPlaying)
            {
                nextVoice = (idx + 1) % voices.Length;
                return voices[idx];
            }
        }

        AudioSource stolen = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;
        return stolen;
    }

    private static AudioClip PickClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];

        AudioClip c = clips[UnityEngine.Random.Range(0, clips.Length)];
        if (c != null) return c;

        foreach (AudioClip other in clips)
            if (other != null) return other;
        return null;
    }

    private AudioSource CreateSource(string objectName, bool loop)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);

        AudioSource s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f; // game 2D
        return s;
    }

    // ------------------------------------------------------------------ Tự nối: Player
    private void TryBindPlayer()
    {
        if (combat != null && controller != null && hitReceiver != null) return;
        if (Time.unscaledTime < nextBindTime) return;
        nextBindTime = Time.unscaledTime + 0.5f;

        if (combat == null)
        {
            combat = FindFirstObjectByType<PlayerCombat>();
            if (combat != null)
            {
                combat.OnNormalShot += HandleNormalShot;
                combat.OnSpecialShot += HandleSpecialShot;
            }
        }

        if (controller == null)
        {
            controller = FindFirstObjectByType<PlayerController>();
            if (controller != null) controller.OnJumpPerformed += HandleJump;
        }

        if (hitReceiver == null)
        {
            hitReceiver = FindFirstObjectByType<PlayerHitReceiver>();
            if (hitReceiver != null) hitReceiver.onPlayerDead.AddListener(HandlePlayerDead);
        }
    }

    private void HandleNormalShot() => PlayInternal(SoundId.NormalShot, 1f);
    private void HandleSpecialShot() => PlayInternal(SoundId.SpecialShot, 1f);
    private void HandleJump() => PlayInternal(SoundId.Jump, 1f);
    private void HandlePlayerDead() => HandleDeath();

    // Rơi xuống hố / đụng chướng ngại đi qua GameManager.GameOver mà không qua PlayerHitReceiver
    private void TrackGameOver()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.IsGameOver) HandleDeath();
    }

    private void HandleDeath()
    {
        if (deathHandled) return;
        deathHandled = true;

        StopInternal(SoundId.DiceRoll);
        PlayInternal(SoundId.Die, 1f);

        // Màn Game Over hiện ra ngay sau đó
        pendingMenuShow = true;
        pendingMenuAt = Time.unscaledTime + gameOverMenuDelay;
    }

    // ------------------------------------------------------------------ Tự nối: luồng menu / xúc xắc
    private void TrackFlow()
    {
        GameFlow flow = GameFlow.Instance;
        if (flow == null) { hasLastState = false; return; }

        GameFlowState state = flow.State;
        if (hasLastState && state == lastState) return;

        bool had = hasLastState;
        GameFlowState prev = lastState;
        lastState = state;
        hasLastState = true;

        // Tap to play / chọn số may mắn là chạm màn hình chứ không phải Button -> tiếng bấm ở đây
        if (had && (prev == GameFlowState.Menu || prev == GameFlowState.PickLuckyNumber))
            PlayInternal(SoundId.ButtonClick, 1f);

        if (state == GameFlowState.Menu || state == GameFlowState.PickLuckyNumber)
            PlayInternal(SoundId.MenuShow, 1f);

        if (state == GameFlowState.DiceRoll)
            PlayInternal(SoundId.DiceRoll, 1f);
        else if (had && prev == GameFlowState.DiceRoll)
            StopInternal(SoundId.DiceRoll);
    }

    private void TrackSettings()
    {
        bool open = SettingsPanel.IsOpen;
        if (open && !settingsWasOpen) PlayInternal(SoundId.MenuShow, 1f);
        settingsWasOpen = open;
    }

    // ------------------------------------------------------------------ Tự nối: tiếng bấm Button UI
    private void ScanButtons()
    {
        if (Time.unscaledTime < nextButtonScan) return;
        nextButtonScan = Time.unscaledTime + 1f; // panel bật/tắt hoặc UI dựng bằng code sau đó vẫn được nối

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in buttons)
        {
            if (b == null || hookedButtons.Contains(b)) continue;
            hookedButtons.Add(b);

            if (ShouldIgnore(b)) continue;
            b.onClick.AddListener(PlayButtonClick);
        }
    }

    private bool ShouldIgnore(Button b)
    {
        if (ignoredButtons != null)
            foreach (Button ignored in ignoredButtons)
                if (ignored != null && ignored == b) return true;

        if (!ignoreGameplayButtons) return false;

        GameFlow flow = GameFlow.Instance;
        if (flow == null || flow.ui == null) return false;

        return IsUnder(b, flow.ui.upButton) ||
               IsUnder(b, flow.ui.downButton) ||
               IsUnder(b, flow.ui.skillButton);
    }

    private static bool IsUnder(Button b, GameObject root)
    {
        return root != null && b.transform.IsChildOf(root.transform);
    }

    // ------------------------------------------------------------------ Editor: luôn đủ 1 ô cho mỗi SoundId
    private void EnsureEntries()
    {
        var list = new List<SoundEntry>();
        if (sounds != null)
            foreach (SoundEntry e in sounds)
                if (e != null) list.Add(e);

        foreach (SoundId id in System.Enum.GetValues(typeof(SoundId)))
        {
            if (list.Exists(e => e.id == id)) continue;

            list.Add(new SoundEntry
            {
                id = id,
                loop = id == SoundId.DiceRoll,
                minInterval = id == SoundId.WallHit ? 0.08f : 0.03f
            });
        }

        foreach (SoundEntry e in list) e.name = e.id.ToString();
        sounds = list.ToArray();
    }
}