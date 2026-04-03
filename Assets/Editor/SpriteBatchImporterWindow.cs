using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SpriteBatchImporterWindow : EditorWindow
{
    private string folderPath = "Assets/PaperDoll/Art/Boots";
    private string namePrefix = "Boots_";

    private Vector2 scroll;
    private List<string> foundAssetPaths = new List<string>();

    // Общие import settings
    private TextureImporterType textureType = TextureImporterType.Sprite;
    private SpriteImportMode spriteImportMode = SpriteImportMode.Single;
    private float pixelsPerUnit = 100f;
    private SpriteMeshType meshType = SpriteMeshType.Tight;
    private FilterMode filterMode = FilterMode.Bilinear;
    private TextureWrapMode wrapMode = TextureWrapMode.Clamp;
    private bool generatePhysicsShape = true;
    private bool mipmaps = false;
    private bool alphaIsTransparency = true;
    private bool isReadable = false;

    // Android override
    private bool overrideAndroid = true;
    private int androidMaxSize = 2048;
    private TextureResizeAlgorithm resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
    private TextureImporterFormat androidFormat = TextureImporterFormat.ASTC_6x6;
    private int compressionQuality = 50; // 0..100

    [MenuItem("Tools/Sprites/Batch Sprite Importer")]
    public static void OpenWindow()
    {
        GetWindow<SpriteBatchImporterWindow>("Batch Sprite Importer");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Поиск спрайтов", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        folderPath = EditorGUILayout.TextField("Folder Path", folderPath);

        if (GUILayout.Button("Select", GUILayout.Width(70)))
        {
            string absPath = EditorUtility.OpenFolderPanel("Select folder inside Assets", Application.dataPath, "");
            if (!string.IsNullOrEmpty(absPath))
            {
                if (absPath.StartsWith(Application.dataPath))
                {
                    folderPath = "Assets" + absPath.Substring(Application.dataPath.Length);
                    SearchAssets();
                }
                else
                {
                    EditorUtility.DisplayDialog("Ошибка", "Папка должна находиться внутри папки Assets.", "OK");
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        namePrefix = EditorGUILayout.TextField("Name Prefix", namePrefix);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Find PNG"))
        {
            SearchAssets();
        }

        GUI.enabled = foundAssetPaths.Count > 0;
        if (GUILayout.Button("Apply Settings To All"))
        {
            ApplySettingsToAll();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        DrawSettingsBlock();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Найдено: {foundAssetPaths.Count}", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawFoundAssets();
        EditorGUILayout.EndScrollView();
    }

    private void DrawSettingsBlock()
    {
        EditorGUILayout.LabelField("Import Settings", EditorStyles.boldLabel);

        textureType = (TextureImporterType)EditorGUILayout.EnumPopup("Texture Type", textureType);
        spriteImportMode = (SpriteImportMode)EditorGUILayout.EnumPopup("Sprite Mode", spriteImportMode);
        pixelsPerUnit = EditorGUILayout.FloatField("Pixels Per Unit", pixelsPerUnit);
        meshType = (SpriteMeshType)EditorGUILayout.EnumPopup("Mesh Type", meshType);
        generatePhysicsShape = EditorGUILayout.Toggle("Generate Physics Shape", generatePhysicsShape);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
        wrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup("Wrap Mode", wrapMode);
        filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", filterMode);
        mipmaps = EditorGUILayout.Toggle("Generate Mip Maps", mipmaps);
        alphaIsTransparency = EditorGUILayout.Toggle("Alpha Is Transparency", alphaIsTransparency);
        isReadable = EditorGUILayout.Toggle("Read/Write Enabled", isReadable);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Android Override", EditorStyles.boldLabel);
        overrideAndroid = EditorGUILayout.Toggle("Override For Android", overrideAndroid);
        androidMaxSize = EditorGUILayout.IntPopup(
            "Max Size",
            androidMaxSize,
            new[] { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" },
            new[] { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 });

        resizeAlgorithm = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup("Resize Algorithm", resizeAlgorithm);
        androidFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", androidFormat);
        compressionQuality = EditorGUILayout.IntSlider("Compression Quality", compressionQuality, 0, 100);
    }

    private void DrawFoundAssets()
    {
        const int previewSize = 64;

        foreach (string path in foundAssetPaths)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            Texture2D preview = AssetPreview.GetAssetPreview(asset);
            if (preview == null)
                preview = AssetPreview.GetMiniThumbnail(asset);

            EditorGUILayout.BeginHorizontal("box");
            GUILayout.Label(preview, GUILayout.Width(previewSize), GUILayout.Height(previewSize));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(System.IO.Path.GetFileName(path), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(path);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping", GUILayout.Width(60)))
            {
                EditorGUIUtility.PingObject(asset);
            }

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = asset;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
    }

    private void SearchAssets()
    {
        foundAssetPaths.Clear();

        if (string.IsNullOrWhiteSpace(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("Ошибка", "Укажи корректный путь к папке внутри Assets.", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                continue;

            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

            if (!string.IsNullOrEmpty(namePrefix) && !fileName.StartsWith(namePrefix))
                continue;

            foundAssetPaths.Add(path);
        }

        foundAssetPaths = foundAssetPaths.OrderBy(p => p).ToList();
        Repaint();
    }

    private void ApplySettingsToAll()
    {
        if (foundAssetPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Нет файлов", "Сначала найди PNG-файлы.", "OK");
            return;
        }

        try
        {
            AssetDatabase.StartAssetEditing();

            int changedCount = 0;

            foreach (string path in foundAssetPaths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = textureType;
                importer.spriteImportMode = spriteImportMode;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.wrapMode = wrapMode;
                importer.filterMode = filterMode;
                importer.mipmapEnabled = mipmaps;
                importer.alphaIsTransparency = alphaIsTransparency;
                importer.isReadable = isReadable;
                importer.npotScale = TextureImporterNPOTScale.None;

                TextureImporterSettings texSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(texSettings);

                texSettings.spriteMeshType = meshType;
                texSettings.spriteGenerateFallbackPhysicsShape = generatePhysicsShape;

                importer.SetTextureSettings(texSettings);

                TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
                androidSettings.name = "Android";
                androidSettings.overridden = overrideAndroid;
                androidSettings.maxTextureSize = androidMaxSize;
                androidSettings.resizeAlgorithm = resizeAlgorithm;
                androidSettings.format = androidFormat;
                androidSettings.compressionQuality = compressionQuality;

                importer.SetPlatformTextureSettings(androidSettings);

                importer.SaveAndReimport();
                changedCount++;
            }

            EditorUtility.DisplayDialog("Готово", $"Настройки применены к {changedCount} файлам.", "OK");
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }
    }
}