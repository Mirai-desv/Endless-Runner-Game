
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
        PickUpDice,
        DiceRoll,
        SpecialShoot,
        Complete
    }
    [Header("Game Flow")]
    public GameFlow gameFlow;

    [Header("Tutorial Map")]
    public TutorialChunkDirector chunkDirector;

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

    [Header("Settings")]
    public int requiredJumps = 3;

    [Header("Tutorial Enemy")]
    public float stopDistance = 3.5f;

    private EnemyHealth tutorialEnemy;
    private float originalRunSpeed;
    private bool playerStoppedForEnemy;
    private float nextEnemySearchTime;

    [Header("Platform Detection")]
    public LayerMask platformLayer;

    [Header("Lucky Dice Transition")]
    [SerializeField] private float diceDelay = 3f;

    private bool waitingForDice;

    private bool wasGrounded;
    private bool wasAirborneDuringPlatformStep;

    private bool enemyDefeated;

    public Step CurrentStep { get; private set; }

    private int jumpCount;

    private bool specialShotFired;
    private bool finished;

    void Start()
    {
        if (playerController != null)
            originalRunSpeed = playerController.runSpeed;

        SetStep(Step.Jump);
    }

    void Update()
    {
        bool isShootingStep =
    CurrentStep == Step.NormalShoot ||
    CurrentStep == Step.SpecialShoot;

        if (!isShootingStep || waitingForDice)
            return;

        if (playerController == null)
            return;

        // Nếu chưa tìm thấy quái,
        // tiếp tục tìm khi chunk mới xuất hiện.
        if (tutorialEnemy == null)
        {
            if (Time.time >= nextEnemySearchTime)
            {
                FindTutorialEnemy();
                nextEnemySearchTime = Time.time + 0.25f;
            }

            return;
        }
    }

    private void FindTutorialEnemy()
    {
        EnemyHealth[] enemies =
    FindObjectsByType<EnemyHealth>();

        float closestDistance = float.MaxValue;
        EnemyHealth closest = null;

        float playerX =
            playerController.transform.position.x;

        foreach (EnemyHealth enemy in enemies)
        {
            if (!enemy.gameObject.activeInHierarchy)
                continue;

            float distance =
                enemy.transform.position.x - playerX;

            // Chỉ tìm quái phía trước Player.
            if (distance <= 0f)
                continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }

        if (closest != null)
        {
            tutorialEnemy = closest;

            Debug.Log(
                "[Tutorial] Target enemy: " +
                tutorialEnemy.name
            );
        }
    }

    void FixedUpdate()
    {
        if (playerController == null)
            return;

        if (CurrentStep != Step.ReachPlatform)
            return;

        bool grounded = playerController.IsGrounded;

        // Phải thực sự rời mặt đất trước.
        if (!grounded)
        {
            wasAirborneDuringPlatformStep = true;
        }

        // Phát hiện tiếp đất sau khi nhảy.
        if (grounded &&
            !wasGrounded &&
            wasAirborneDuringPlatformStep)
        {
            if (IsStandingOnPlatform())
            {
                OnPlatformReached();
            }
        }

        wasGrounded = grounded;
    }

    void SetStep(Step step)
    {
        CurrentStep = step;

        // Chọn nhóm chunk theo bước Tutorial.
        if (chunkDirector != null)
            chunkDirector.UseStep(step);

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

                wasGrounded = playerController != null &&
                              playerController.IsGrounded;

                wasAirborneDuringPlatformStep = false;
                break;

            case Step.Drop:
                Show("TAP TO DROP");
                if (progressText != null)
                    progressText.text = "0/1";
                break;

            case Step.CollectMoon:
                Show("COLLECT MOONS TO FILL YOUR POWER BAR");
                break;

            case Step.NormalShoot:
                Show("SHOOT THE ENEMY!");

                tutorialEnemy = null;

                if (progressText != null)
                    progressText.text = "0/1";

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
            case Step.PickUpDice:
                Show("PICK UP THE DICE!");

                if (progressText != null)
                    progressText.text = "0/1";

                break;

            case Step.DiceRoll:
                Show("ROLLING THE DICE!");
                break;


            case Step.SpecialShoot:
                Show("UNLEASH YOUR POWER!");

                tutorialEnemy = null;
                specialShotFired = false;

                nextEnemySearchTime = 0f;

                if (progressText != null)
                    progressText.text = "0/1";

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

    private bool IsStandingOnPlatform()
    {
        Collider2D playerCollider =
            playerController.GetComponent<Collider2D>();

        if (playerCollider == null)
            return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(platformLayer);
        filter.useTriggers = false;

        RaycastHit2D[] hits = new RaycastHit2D[8];

        int count = playerCollider.Cast(
            Vector2.down,
            filter,
            hits,
            0.12f
        );

        for (int i = 0; i < count; i++)
        {
            if (hits[i].collider == null)
                continue;

            if (hits[i].normal.y > 0.5f)
                return true;
        }

        return false;
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

        EnemyHealth.OnAnyEnemyDied += HandleEnemyDied;
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

        EnemyHealth.OnAnyEnemyDied -= HandleEnemyDied;

        ResumePlayer();
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
        if (CurrentStep != Step.NormalShoot)
            return;

        Debug.Log("[Tutorial] Normal shot fired!");
    }

    // Gọi khi cơ chế Special Mode đã sẵn sàng
    private void HandleLuckyNumberChosen(int number)
    {
        if (CurrentStep != Step.ChooseLuckyDice)
            return;

        Debug.Log("[Tutorial] Lucky Number: " + number);

        // Chỉ lưu/chọn số thông qua GameFlow.
        // Chưa kích hoạt Special Mode.
        SetStep(Step.PickUpDice);
    }

    // Gọi khi sử dụng đạn đặc biệt thành công

    public void OnSpecialShoot()
    {
        if (CurrentStep != Step.SpecialShoot)
            return;

        specialShotFired = true;

        Debug.Log(
            "[Tutorial] Special shot registered!"
        );

        Show("DEFEAT THE ENEMY!");
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



    private IEnumerator FinishRoutine()
    {
        // Hiện thông báo hoàn thành trong 2 giây
        yield return new WaitForSecondsRealtime(2f);

        // Lưu tiến độ và chuyển sang Game Scene
        GameStartup.CompleteTutorial();
    }



    private void StopPlayerForShooting()
    {
        if (playerStoppedForEnemy)
            return;

        playerStoppedForEnemy = true;

        // Chỉ dừng chạy ngang.
        // Player vẫn có thể nhảy, drop và bắn.
        playerController.runSpeed = 0f;

        Debug.Log(
            "[Tutorial] Stop and shoot the enemy!"
        );
    }

    private void ResumePlayer()
    {
        if (playerController == null)
            return;

        if (!playerStoppedForEnemy)
            return;

        playerController.runSpeed = originalRunSpeed;
        playerStoppedForEnemy = false;
    }

    public bool ShouldIgnoreEnemyContact(EnemyHealth enemy)
    {
        return true;

    }


    private void HandleEnemyDied(EnemyHealth enemy)
    {
        if (enemy == null)
            return;

        // NORMAL SHOOT
        if (CurrentStep == Step.NormalShoot)
        {
            if (waitingForDice)
                return;

            Debug.Log("[Tutorial] Normal enemy defeated!");

            tutorialEnemy = null;
            waitingForDice = true;

            if (progressText != null)
                progressText.text = "1/1";

            Show("GREAT SHOT!");

            StartCoroutine(WaitForLuckyDice());
            return;
        }

        // SPECIAL SHOOT
        if (CurrentStep == Step.SpecialShoot)
        {
            // Player phải sử dụng Special ít nhất một lần.
            if (!specialShotFired)
                return;

            Debug.Log("[Tutorial] Special enemy defeated!");

            tutorialEnemy = null;

            if (progressText != null)
                progressText.text = "1/1";

            SetStep(Step.Complete);
        }
    }

    private IEnumerator WaitForLuckyDice()
    {
        // Player vẫn chạy trong thời gian chờ.
        yield return new WaitForSeconds(diceDelay);

        if (CurrentStep != Step.NormalShoot)
            yield break;

        waitingForDice = false;

        SetStep(Step.ChooseLuckyDice);
    }

    // Gọi khi Player thực sự nhặt Dice.
    public void OnTutorialDiceCollected()
    {
        if (CurrentStep != Step.PickUpDice)
            return;

        Debug.Log("[Tutorial] Dice collected!");

        SetStep(Step.DiceRoll);
    }

    // Gọi sau khi Dice quay XONG và hiện kết quả.

    public void OnTutorialDiceRollCompleted()
    {
        if (CurrentStep != Step.DiceRoll)
            return;

        Debug.Log("[Tutorial] Dice roll completed!");

        // DicePickup đã kích hoạt Lucky.
        // Không cộng thưởng lần thứ hai.
        SetStep(Step.SpecialShoot);
    }
    public void SkipTutorial()
    {
        Debug.Log("Tutorial skipped!");

        GameStartup.CompleteTutorial();
    }
}
