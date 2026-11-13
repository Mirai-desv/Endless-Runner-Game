
using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public enum Step
    {
        Jump,
        ReachPlatform,
        Drop,
        CollectMoon,
        NormalShoot,
        ChooseLuckyDice,
        SpecialShoot,
        Complete
    }
    [Header("Game Flow")]
    public GameFlow gameFlow;

    [Header("Gameplay References")]
    public PlayerController playerController;
    public PlayerCombat playerCombat;
    public EnergySystem energySystem;

    [Header("UI")]
    public TMP_Text instructionText;
    public TMP_Text progressText;

    public GameObject jumpArrow;
    public GameObject dropArrow;
    public GameObject energyArrow;
    public GameObject shootArrow;

    [Header("Platform")]
    public GameObject tutorialPlatformPrefab;
    public Transform player;
    public float spawnAhead = 8f;
    public float platformHeight = 2.5f;

    [Header("Settings")]
    public int requiredJumps = 3;

    public Step CurrentStep { get; private set; }

    private int jumpCount;
    private bool finished;

    void Start()
    {
        SetStep(Step.Jump);
    }

    void SetStep(Step step)
    {
        CurrentStep = step;

        if (jumpArrow != null)
            jumpArrow.SetActive(step == Step.Jump ||
                step == Step.ReachPlatform);

        if (dropArrow != null)
            dropArrow.SetActive(step == Step.Drop);

        if (energyArrow != null)
            energyArrow.SetActive(step == Step.CollectMoon);

        if (shootArrow != null)
            shootArrow.SetActive(step == Step.NormalShoot ||
                step == Step.SpecialShoot);

        if (progressText != null)
            progressText.text = "";

        switch (step)
        {
            case Step.Jump:
                Show("TAP TO JUMP");
                UpdateJumpProgress();
                break;

            case Step.ReachPlatform:
                Show("JUMP ON THE PLATFORM!");
                SpawnTutorialPlatform();
                break;

            case Step.Drop:
                Show("TAP TO DROP");
                progressText.text = "0/1";
                break;

            case Step.CollectMoon:
                Show("COLLECT MOONS TO FILL YOUR POWER BAR");
                break;

            case Step.NormalShoot:
                Show("TAP TO SHOOT!");
                break;

            case Step.ChooseLuckyDice:
                Show("CHOOSE YOUR LUCKY NUMBER!");

                if (gameFlow != null)
                {
                    if (!gameFlow.OpenTutorialLuckyPicker())
                    {
                        Debug.LogWarning(
                            "[Tutorial] Cannot open Lucky Picker."
                        );
                    }
                }
                break;

            case Step.SpecialShoot:
                Show("UNLEASH YOUR POWER!");
                break;

            case Step.Complete:
                CompleteTutorial();
                break;
        }
    }

    void Show(string message)
    {
        if (instructionText != null)
            instructionText.text = message;
    }

    void UpdateJumpProgress()
    {
        if (progressText != null)
            progressText.text =
                jumpCount + "/" + requiredJumps;
    }

    void OnEnable()
    {
        if (playerController != null)
        {
            playerController.OnJumpPerformed += OnJump;
            playerController.OnDropPerformed += OnDrop;
        }

        if (playerCombat != null)
        {
            playerCombat.OnNormalShot += OnNormalShoot;
            playerCombat.OnSpecialShot += OnSpecialShoot;
        }

        if (energySystem != null)
        {
            energySystem.OnEnergyChanged += HandleEnergyChanged;
        }

        if (gameFlow != null)
        {
            gameFlow.OnTutorialLuckyNumberChosen +=
                HandleLuckyNumberChosen;
        }
    }

    void OnDisable()
    {
        if (playerController != null)
        {
            playerController.OnJumpPerformed -= OnJump;
            playerController.OnDropPerformed -= OnDrop;
        }

        if (playerCombat != null)
        {
            playerCombat.OnNormalShot -= OnNormalShoot;
            playerCombat.OnSpecialShot -= OnSpecialShoot;
        }

        if (energySystem != null)
        {
            energySystem.OnEnergyChanged -= HandleEnergyChanged;
        }

        if (gameFlow != null)
        {
            gameFlow.OnTutorialLuckyNumberChosen -=
                HandleLuckyNumberChosen;
        }
    }

    void HandleEnergyChanged()
    {
        if (energySystem == null || energySystem.maxEnergy <= 0)
            return;

        float normalizedEnergy =
            (float)energySystem.currentEnergy /
            energySystem.maxEnergy;

        OnEnergyChanged(normalizedEnergy);
    }

    // Gọi khi nhân vật nhảy thành công
    public void OnJump()
    {
        if (CurrentStep != Step.Jump)
            return;

        jumpCount++;
        UpdateJumpProgress();

        if (jumpCount >= requiredJumps)
            SetStep(Step.ReachPlatform);
    }

    // Gọi khi nhân vật đáp lên tutorial platform
    public void OnPlatformReached()
    {
        if (CurrentStep == Step.ReachPlatform)
            SetStep(Step.Drop);
    }

    // Gọi khi nhân vật drop thành công
    public void OnDrop()
    {
        if (CurrentStep == Step.Drop)
            SetStep(Step.CollectMoon);
    }

    // normalizedEnergy là giá trị 0..1 của thanh năng lượng
    public void OnEnergyChanged(float normalizedEnergy)
    {
        if (CurrentStep != Step.CollectMoon)
            return;

        if (normalizedEnergy >= 0.999f)
            SetStep(Step.NormalShoot);
    }

    // Gọi khi bắn thường thành công
    public void OnNormalShoot()
    {
        if (CurrentStep == Step.NormalShoot)
            SetStep(Step.ChooseLuckyDice);
    }

    // Gọi khi cơ chế Special Mode đã sẵn sàng
       private void HandleLuckyNumberChosen(int number)
    {
        if (CurrentStep != Step.ChooseLuckyDice)
            return;

        Debug.Log("[Tutorial] Lucky Number: " + number);

        // Cấp Special Mode để người chơi tập bắn.
        if (energySystem != null)
        {
            energySystem.ActivateLuckyDice();
        }

        SetStep(Step.SpecialShoot);
    }

    // Gọi khi sử dụng đạn đặc biệt thành công
    public void OnSpecialShoot()
    {
        if (CurrentStep == Step.SpecialShoot)
            SetStep(Step.Complete);
    }

    void SpawnTutorialPlatform()
    {
        if (tutorialPlatformPrefab == null || player == null)
            return;

        Vector3 position = player.position +
            new Vector3(spawnAhead, platformHeight, 0f);

        Instantiate(tutorialPlatformPrefab, position,
            Quaternion.identity);
    }

    void CompleteTutorial()
    {
        if (finished) return;
        finished = true;

        Show("TUTORIAL COMPLETE!");

        if (progressText != null)
        progressText.text = "";

        StartCoroutine(FinishRoutine());
    }

    IEnumerator FinishRoutine()
    {
        yield return new WaitForSecondsRealtime(2f);

        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
    }
}
