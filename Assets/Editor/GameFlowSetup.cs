using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu Tools > Endless Runner > Create Game Flow UI in Scene.
/// Tạo sẵn trong scene hiện tại: Canvas "GameFlowUI" (3 panel) + object "GameFlow" đã nối đủ tham chiếu,
/// và xuất 6 ảnh xúc xắc ra Assets/Art/DiceFaces để bạn thay bằng ảnh của mình.
/// </summary>
public static class GameFlowSetup
{
    private const string FacesFolder = "Assets/Art/DiceFaces";

    [MenuItem("Tools/Endless Runner/Create Game Flow UI in Scene")]
    public static void CreateInScene()
    {
        if (Object.FindFirstObjectByType<GameFlow>() != null)
        {
            EditorUtility.DisplayDialog(
                "Game Flow",
                "Scene này đã có object GameFlow rồi. Hãy xóa nó (và Canvas GameFlowUI) nếu muốn tạo lại từ đầu.",
                "OK");
            return;
        }

        Sprite[] faces = ExportDiceFaces();

        // Dùng ảnh vừa xuất (hoặc ảnh bạn đã có sẵn trong thư mục) cho UI mặc định.
        DiceFaces.SetInspectorSprites(faces);
        GameObject canvas = GameFlowLayout.CreateCanvas(null, "GameFlowUI");
        GameFlowUIRefs ui = GameFlowLayout.Build(canvas.transform, "ENDLESS RUNNER", "TAP TO PLAY");
        DiceFaces.SetInspectorSprites(null);

        GameObject flowGo = new GameObject("GameFlow");
        GameFlow flow = flowGo.AddComponent<GameFlow>();
        flow.ui = ui;
        flow.diceFaceSprites = faces;

        // Chỉ để panel Tap to play hiện sẵn cho dễ chỉnh; 2 panel còn lại tắt đi (bật lại bằng ô tick khi muốn sửa).
        // Lúc chạy game, GameFlow tự ẩn/hiện đúng panel.
        ui.luckyPanel.SetActive(false);
        ui.rollPanel.SetActive(false);

        Undo.RegisterCreatedObjectUndo(canvas, "Create Game Flow UI");
        Undo.RegisterCreatedObjectUndo(flowGo, "Create Game Flow UI");
        EditorSceneManager.MarkSceneDirty(flowGo.scene);
        Selection.activeGameObject = flowGo;

        Debug.Log("[GameFlow] Đã tạo Canvas 'GameFlowUI' và 'GameFlow'. Chỉnh UI trong Hierarchy, " +
                  "ảnh xúc xắc nằm ở " + FacesFolder + ". Nhớ lưu scene (Ctrl+S).", flowGo);
    }

    // Xuất 6 PNG nếu chưa có (không ghi đè ảnh bạn đã thay), rồi trả về 6 sprite.
    private static Sprite[] ExportDiceFaces()
    {
        Directory.CreateDirectory(FacesFolder);

        Sprite[] result = new Sprite[6];
        for (int face = 1; face <= 6; face++)
        {
            string path = FacesFolder + "/dice_" + face + ".png";

            if (!File.Exists(path))
            {
                Texture2D tex = DiceFaces.BuildTexture(face, true);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }

            result[face - 1] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        AssetDatabase.Refresh();
        return result;
    }
}
