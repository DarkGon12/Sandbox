using UnityEditor;
using UnityEngine;
using Creatures;
using System.Collections.Generic;

public class CreatureMenuWindow : EditorWindow
{
    CreaturesRepository repository;

    Vector2 scroll;
    string search = "";

    int cardSize = 120;
    int selectedIndex = -1;

    [MenuItem("Sandbox/Add creatures")]
    public static void Open()
    {
        GetWindow<CreatureMenuWindow>("Creature menu");
    }

    void OnEnable()
    {
        repository = AssetDatabase.LoadAssetAtPath<CreaturesRepository>(
            "Assets/_DATA/Creature/CreatureRepository.asset");
    }

    void OnGUI()
    {
        DrawToolbar();
        DrawSearch();

        if (repository == null)
        {
            EditorGUILayout.HelpBox("Repository not found", MessageType.Error);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawGrid();

        EditorGUILayout.EndScrollView();
    }

    void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Create", EditorStyles.toolbarButton))
            CreatureCreateWindow.Open(repository);

        if (GUILayout.Button("Delete", EditorStyles.toolbarButton))
            DeleteSelected();

        if (GUILayout.Button("Update", EditorStyles.toolbarButton))
        {
            EditorUtility.SetDirty(repository);
            AssetDatabase.SaveAssets();
        }

        if (GUILayout.Button("Show", EditorStyles.toolbarButton))
            ShowSelected();

        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();
    }

    void DrawSearch()
    {
        GUILayout.BeginHorizontal("box");

        GUILayout.Label("Search", GUILayout.Width(50));

        search = GUILayout.TextField(search);

        GUILayout.EndHorizontal();
    }

    void DrawGrid()
    {
        int columns = Mathf.Max(1, Mathf.FloorToInt(position.width / (cardSize + 20)));

        int index = 0;

        List<CreaturesData> list = repository.CreaturesCollection;

        while (index < list.Count)
        {
            GUILayout.BeginHorizontal();

            for (int i = 0; i < columns; i++)
            {
                if (index >= list.Count)
                    break;

                var creature = list[index];

                if (!string.IsNullOrEmpty(search) &&
                    !creature.GetUIInfo.CreatureName.ToLower().Contains(search.ToLower()))
                {
                    index++;
                    i--;
                    continue;
                }

                DrawCard(creature, index);

                index++;
            }

            GUILayout.EndHorizontal();
        }
    }

    void DrawCard(CreaturesData creature, int index)
    {
        GUIStyle card = new GUIStyle("box");

        if (selectedIndex == index)
            card.normal.background = Texture2D.grayTexture;

        GUILayout.BeginVertical(card,
            GUILayout.Width(cardSize),
            GUILayout.Height(cardSize + 40));

        Texture preview = null;

        if (creature.GetObject != null)
            preview = AssetPreview.GetAssetPreview(creature.GetObject);

        if (preview == null && creature.GetUIInfo.CreatureIcon != null)
            preview = AssetPreview.GetAssetPreview(creature.GetUIInfo.CreatureIcon);

        GUILayout.Label(preview, GUILayout.Width(cardSize), GUILayout.Height(cardSize));

        GUILayout.Label(creature.GetUIInfo.CreatureName,
            EditorStyles.boldLabel,
            GUILayout.Width(cardSize));

        if (GUILayout.Button("Select"))
        {
            selectedIndex = index;
            Selection.activeObject = creature;
        }

        GUILayout.EndVertical();
    }

    void DeleteSelected()
    {
        if (selectedIndex < 0) return;

        var data = repository.CreaturesCollection[selectedIndex];

        repository.CreaturesCollection.RemoveAt(selectedIndex);

        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(data));

        selectedIndex = -1;

        EditorUtility.SetDirty(repository);
        AssetDatabase.SaveAssets();
    }

    void ShowSelected()
    {
        if (selectedIndex < 0) return;

        var data = repository.CreaturesCollection[selectedIndex];

        EditorGUIUtility.PingObject(data);
        Selection.activeObject = data;
    }
}