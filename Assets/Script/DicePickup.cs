
using UnityEngine;
using EndlessRunner.Run.World;

public class DicePickup : MonoBehaviour, IChunkResettable
{
    [Range(1, 6)]
    public int luckyNumber = 4;

    public int normalReward = 25;

    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        EnergySystem energy =
            other.GetComponentInParent<EnergySystem>();

        if (energy == null) return;

        GameFlow flow = GameFlow.Instance;

        TutorialManager tutorial =
            FindAnyObjectByType<TutorialManager>();

        bool isTutorial = flow != null && flow.tutorialMode;

        if (isTutorial &&
            (tutorial == null ||
             tutorial.CurrentStep != TutorialManager.Step.PickUpDice))
        {
            Debug.LogWarning(
                "[Tutorial] Dice collected at wrong step!"
            );
            return;
        }

        if (flow != null)
        {
            int lucky = flow.LuckyNumber;
            int reward = normalReward;

            bool started = flow.StartDiceRoll(
                reward,
                result =>
                {
                    if (energy == null) return;

                    // Trong Tutorial luôn cấp Lucky.
                    int finalResult =
                        isTutorial ? lucky : result;

                    ApplyResult(
                        energy,
                        finalResult,
                        lucky,
                        reward
                    );

                    // Quay xong mới chuyển bước.
                    if (isTutorial && tutorial != null)
                    {
                        tutorial
                            .OnTutorialDiceRollCompleted();
                    }
                }
            );

            if (!started)
                return;
        }
        else
        {
            int result = isTutorial
                ? luckyNumber
                : Random.Range(1, 7);

            ApplyResult(
                energy,
                result,
                luckyNumber,
                normalReward
            );
        }

        triggered = true;

        // Phải thông báo sau khi StartDiceRoll thành công.
        if (isTutorial && tutorial != null)
        {
            tutorial.OnTutorialDiceCollected();

            // Trường hợp không có GameFlow.
            if (flow == null)
            {
                tutorial.OnTutorialDiceRollCompleted();
            }
        }

        gameObject.SetActive(false);
    }

    public void ResetForReuse()
    {
        triggered = false;
        gameObject.SetActive(true);
    }

    private static void ApplyResult(
        EnergySystem energy,
        int result,
        int lucky,
        int reward
    )
    {
        if (energy == null) return;

        Debug.Log(
            $"[Dice] Result={result}, Lucky={lucky}"
        );

        if (result == lucky)
        {
            energy.ActivateLuckyDice();
            Debug.Log("[Dice] LUCKY!");
        }
        else
        {
            energy.AddEnergy(reward);
        }
    }
}
