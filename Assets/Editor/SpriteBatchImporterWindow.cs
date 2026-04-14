using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public class SpriteBatchImporterWindow : EditorWindow
{
    private string folderPath = "Assets/_PaperDoll/Art/Boots";
    private string namePrefix = "";

    private bool onlySpriteToggle = false;
    private bool onlyUiToggle = false;
    private bool showIconsToggle = true;
    private bool showAllToggle = false;

    private Vector2 scroll;

    private const int ItemsPerPage = 50;
    private int currentPage = 0;

    private enum SortMode
    {
        Name,
        Path,
        BuildSize
    }

    private SortMode sortMode = SortMode.Name;
    private bool sortAscending = true;

    private List<SpriteAssetInfo> foundAssets = new List<SpriteAssetInfo>();

    // Android override
    private bool overrideAndroid = true;
    private int androidMaxSize = 2048;
    private TextureResizeAlgorithm resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
    private TextureImporterFormat androidFormat = TextureImporterFormat.ASTC_6x6;
    private int compressionQuality = 50;

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
                if (absPath.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
                {
                    folderPath = "Assets" + absPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    EditorUtility.DisplayDialog("Ошибка", "Папка должна находиться внутри папки Assets.", "OK");
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        namePrefix = EditorGUILayout.TextField("Name Prefix", namePrefix);

        EditorGUI.BeginChangeCheck();
        onlyUiToggle = EditorGUILayout.Toggle("Только UI", onlyUiToggle);
        onlySpriteToggle = EditorGUILayout.Toggle("Только не UI", onlySpriteToggle);
        showIconsToggle = EditorGUILayout.Toggle("Показывать иконки", showIconsToggle);
        showAllToggle = EditorGUILayout.Toggle("Отобразить всё", showAllToggle);
        if (EditorGUI.EndChangeCheck())
        {
            currentPage = 0;
            SearchAssets();
            Repaint();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Find PNG"))
        {
            currentPage = 0;
            SearchAssets();
        }

        GUI.enabled = foundAssets.Count > 0;
        if (GUILayout.Button("Apply Settings To All"))
        {
            ApplySettingsToAll();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        DrawSettingsBlock();

        EditorGUILayout.Space();
        DrawSortBlock();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Найдено: {foundAssets.Count}", EditorStyles.boldLabel);

        DrawPaginationBlock();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawFoundAssets();
        EditorGUILayout.EndScrollView();

        DrawPaginationBlock();
    }

    private void DrawSettingsBlock()
    {
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

    private void DrawSortBlock()
    {
        EditorGUILayout.LabelField("Сортировка", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        var newSortMode = (SortMode)EditorGUILayout.EnumPopup("Сортировать по", sortMode);
        if (newSortMode != sortMode)
        {
            sortMode = newSortMode;
            ApplySorting();
        }

        bool newSortAscending = EditorGUILayout.ToggleLeft("По возрастанию", sortAscending, GUILayout.Width(130));
        if (newSortAscending != sortAscending)
        {
            sortAscending = newSortAscending;
            ApplySorting();
        }

        if (GUILayout.Button("Обновить вес", GUILayout.Width(120)))
        {
            RefreshBuildSizes();
            ApplySorting();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawPaginationBlock()
    {
        if (showAllToggle || foundAssets.Count == 0)
            return;

        int totalPages = Mathf.CeilToInt(foundAssets.Count / (float)ItemsPerPage);
        totalPages = Mathf.Max(totalPages, 1);
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        EditorGUILayout.BeginHorizontal("box");

        GUI.enabled = currentPage > 0;
        if (GUILayout.Button("<<", GUILayout.Width(40)))
        {
            currentPage = 0;
            scroll = Vector2.zero;
        }

        if (GUILayout.Button("<", GUILayout.Width(40)))
        {
            currentPage--;
            scroll = Vector2.zero;
        }

        GUI.enabled = true;
        GUILayout.Label($"Страница {currentPage + 1} / {totalPages}", GUILayout.Width(140));

        GUI.enabled = currentPage < totalPages - 1;
        if (GUILayout.Button(">", GUILayout.Width(40)))
        {
            currentPage++;
            scroll = Vector2.zero;
        }

        if (GUILayout.Button(">>", GUILayout.Width(40)))
        {
            currentPage = totalPages - 1;
            scroll = Vector2.zero;
        }

        GUI.enabled = true;
        GUILayout.FlexibleSpace();

        int from = currentPage * ItemsPerPage + 1;
        int to = Mathf.Min((currentPage + 1) * ItemsPerPage, foundAssets.Count);
        GUILayout.Label($"{from}-{to} из {foundAssets.Count}");

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFoundAssets()
    {
        const int previewSize = 128;

        IEnumerable<SpriteAssetInfo> assetsToDraw = foundAssets;

        if (!showAllToggle)
        {
            assetsToDraw = foundAssets
                .Skip(currentPage * ItemsPerPage)
                .Take(ItemsPerPage);
        }

        foreach (SpriteAssetInfo info in assetsToDraw)
        {
            UnityEngine.Object asset = info.Asset;

            EditorGUILayout.BeginHorizontal("box");

            if (showIconsToggle)
            {
                Texture2D preview = AssetPreview.GetAssetPreview(asset);
                if (preview == null)
                    preview = AssetPreview.GetMiniThumbnail(asset);

                GUILayout.Label(preview, GUILayout.Width(previewSize), GUILayout.Height(previewSize));
            }

            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField(info.FileName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(info.AssetPath);

            EditorGUILayout.LabelField("Вес в билде / памяти:", FormatBytes(info.BuildSizeBytes));
            EditorGUILayout.LabelField("Размер файла на диске:", FormatBytes(info.DiskSizeBytes));

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
        foundAssets.Clear();

        if (string.IsNullOrWhiteSpace(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("Ошибка", "Укажи корректный путь к папке внутри Assets.", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                continue;

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);

            if (!string.IsNullOrEmpty(namePrefix) &&
                !fileNameWithoutExtension.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (onlyUiToggle &&
                fileNameWithoutExtension.IndexOf("UI", StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (onlySpriteToggle &&
    fileNameWithoutExtension.IndexOf("UI", StringComparison.OrdinalIgnoreCase) > 0)
                continue;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
                continue;

            foundAssets.Add(new SpriteAssetInfo
            {
                AssetPath = path,
                FileName = Path.GetFileName(path),
                Asset = texture,
                DiskSizeBytes = GetAssetFileSize(path),
                BuildSizeBytes = GetTextureBuildSize(texture)
            });
        }

        ApplySorting();
        currentPage = 0;
        Repaint();
    }

    private void RefreshBuildSizes()
    {
        foreach (SpriteAssetInfo info in foundAssets)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(info.AssetPath);
            info.Asset = texture;
            info.BuildSizeBytes = texture != null ? GetTextureBuildSize(texture) : -1;
            info.DiskSizeBytes = GetAssetFileSize(info.AssetPath);
        }
    }

    private void ApplySorting()
    {
        IEnumerable<SpriteAssetInfo> query = foundAssets;

        switch (sortMode)
        {
            case SortMode.Name:
                query = sortAscending
                    ? query.OrderBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
                    : query.OrderByDescending(x => x.FileName, StringComparer.OrdinalIgnoreCase);
                break;

            case SortMode.Path:
                query = sortAscending
                    ? query.OrderBy(x => x.AssetPath, StringComparer.OrdinalIgnoreCase)
                    : query.OrderByDescending(x => x.AssetPath, StringComparer.OrdinalIgnoreCase);
                break;

            case SortMode.BuildSize:
                query = sortAscending
                    ? query.OrderBy(x => x.BuildSizeBytes)
                    : query.OrderByDescending(x => x.BuildSizeBytes);
                break;
        }

        foundAssets = query.ToList();

        int totalPages = Mathf.CeilToInt(foundAssets.Count / (float)ItemsPerPage);
        totalPages = Mathf.Max(totalPages, 1);
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        Repaint();
    }

    private long GetTextureBuildSize(Texture texture)
    {
        if (texture == null)
            return -1;

        try
        {
            return Profiler.GetRuntimeMemorySizeLong(texture);
        }
        catch
        {
            return -1;
        }
    }

    private long GetAssetFileSize(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return -1;

        string fullPath = Path.Combine(projectRoot, assetPath);

        if (!File.Exists(fullPath))
            return -1;

        return new FileInfo(fullPath).Length;
    }

    private string FormatBytes(long bytes)
    {
        if (bytes < 0)
            return "-";

        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        int unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        size /= 2;
        return $"{size:0.##} {units[unitIndex]}";
    }

    private void ApplySettingsToAll()
    {
        if (foundAssets.Count == 0)
        {
            EditorUtility.DisplayDialog("Нет файлов", "Сначала найди PNG-файлы.", "OK");
            return;
        }

        try
        {
            AssetDatabase.StartAssetEditing();

            int changedCount = 0;

            foreach (SpriteAssetInfo info in foundAssets)
            {
                TextureImporter importer = AssetImporter.GetAtPath(info.AssetPath) as TextureImporter;
                if (importer == null)
                    continue;

                importer.npotScale = TextureImporterNPOTScale.None;

                TextureImporterSettings texSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(texSettings);
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

            RefreshBuildSizes();
            ApplySorting();

            EditorUtility.DisplayDialog("Готово", $"Настройки применены к {changedCount} файлам.", "OK");
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }
    }

    [Serializable]
    private class SpriteAssetInfo
    {
        public string AssetPath;
        public string FileName;
        public UnityEngine.Object Asset;
        public long DiskSizeBytes;
        public long BuildSizeBytes;
    }
}