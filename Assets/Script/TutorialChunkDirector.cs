
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using EndlessRunner.Run.World;

[DefaultExecutionOrder(-1000)]
public class TutorialChunkDirector : MonoBehaviour
{
    [Header("Generator")]
    [SerializeField] private ChunkGenerator generator;

    [Header("Tutorial Library")]
    public ChunkTutLib tutorialLibrary;

    private ChunkLibrary runtimeLibrary;
    private FieldInfo libraryField;

    private void Awake()
    {
        if (generator == null)
            generator = GetComponent<ChunkGenerator>();

        if (generator == null)
        {
            Debug.LogError(
                "[Tutorial Map] Missing ChunkGenerator!");
            enabled = false;
            return;
        }

        if (tutorialLibrary == null ||
            tutorialLibrary.chunks == null ||
            tutorialLibrary.chunks.Count == 0)
        {
            Debug.LogError(
                "[Tutorial Map] Missing or empty ChunkTutLib!");
            enabled = false;
            return;
        }

        libraryField = typeof(ChunkGenerator).GetField(
            "library",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (libraryField == null)
        {
            Debug.LogError(
                "[Tutorial Map] Cannot find library field!");
            enabled = false;
            return;
        }

        ChunkLibrary original =
            libraryField.GetValue(generator) as ChunkLibrary;

        runtimeLibrary = original != null
            ? Instantiate(original)
            : ScriptableObject.CreateInstance<ChunkLibrary>();

        runtimeLibrary.hideFlags = HideFlags.DontSave;

        // Chỉ thay thư viện runtime, không sửa asset gốc.
        libraryField.SetValue(generator, runtimeLibrary);

        // Bắt đầu với chunk đầu tiên.
        SetChunkByIndex(0);
    }

    public void UseStep(TutorialManager.Step step)
    {
        int index = step switch
        {
            TutorialManager.Step.Jump => 0,

            TutorialManager.Step.ReachPlatform => 1,

            TutorialManager.Step.Drop => 1,
            TutorialManager.Step.CollectMoon => 1,

            TutorialManager.Step.NormalShoot => 2,

            TutorialManager.Step.ChooseLuckyDice => 3,
            TutorialManager.Step.PickUpDice => 3,
            TutorialManager.Step.DiceRoll => 3,
            TutorialManager.Step.SpecialShoot => 3,

            _ => -1
        };

        if (index >= 0)
            SetChunkByIndex(index);
    }

    private void SetChunkByIndex(int index)
    {
        if (runtimeLibrary == null ||
            tutorialLibrary == null)
            return;

        if (index < 0 ||
            index >= tutorialLibrary.chunks.Count)
        {
            Debug.LogWarning(
                $"[Tutorial Map] Invalid chunk index: {index}");
            return;
        }

        ChunkData selected = tutorialLibrary.chunks[index];

        if (selected == null || selected.prefab == null)
        {
            Debug.LogWarning(
                $"[Tutorial Map] Invalid ChunkData at index {index}");
            return;
        }

        // Generator chỉ được chọn chunk của giai đoạn này.
        runtimeLibrary.chunks = new List<ChunkData>
        {
            selected
        };

        Debug.Log(
            $"[Tutorial Map] Step chunk: {index} | " +
            $"Data: {selected.name}"
        );
    }

    private void OnDestroy()
    {
        if (runtimeLibrary != null)
        {
            Destroy(runtimeLibrary);
        }
    }
}
