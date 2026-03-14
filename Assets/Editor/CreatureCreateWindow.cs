using UnityEditor;
using UnityEngine;
using Creatures;
using UnityEngine.AI;

public class CreatureCreateWindow : EditorWindow
{
    CreaturesRepository repository;

    CreaturesType type;

    int weaponIndex;
    string creatureName;
    string mapIndex;

    float moveSpeed = 1;
    int attackPower = 10;

    Sprite icon;
    string price;
    bool rewarded;

    GameObject targetObject;

    Vector2 scroll;

    public static void Open(CreaturesRepository repo)
    {
        var window = GetWindow<CreatureCreateWindow>("Create Creature");
        window.repository = repo;
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        GUILayout.Space(10);

        EditorGUILayout.LabelField("CREATURE CREATOR",
            EditorStyles.boldLabel);

        GUILayout.Space(10);

        type = (CreaturesType)EditorGUILayout.EnumPopup("Creature type", type);

        EditorGUILayout.Space(10);

        DrawTypeFields();

        EditorGUILayout.Space(10);

        DrawStats();

        EditorGUILayout.Space(10);

        DrawUI();

        EditorGUILayout.Space(10);

        DrawTarget();

        GUILayout.Space(20);

        if (GUILayout.Button("CREATE", GUILayout.Height(40)))
            CreateCreature();

        EditorGUILayout.EndScrollView();
    }

    void DrawTypeFields()
    {
        GUILayout.Label("Basic Info", EditorStyles.boldLabel);

        creatureName = EditorGUILayout.TextField("Name", creatureName);

        if (type == CreaturesType.Weapon)
            weaponIndex = EditorGUILayout.IntField("Weapon index", weaponIndex);

        if (type == CreaturesType.Map)
            mapIndex = EditorGUILayout.TextField("Map index", mapIndex);
    }

    void DrawStats()
    {
        if (type == CreaturesType.Map || type == CreaturesType.Weapon)
            return;

        GUILayout.Label("Stats", EditorStyles.boldLabel);

        moveSpeed = EditorGUILayout.FloatField("Move Speed", moveSpeed);
        attackPower = EditorGUILayout.IntField("Attack Power", attackPower);
    }

    void DrawUI()
    {
        GUILayout.Label("UI", EditorStyles.boldLabel);

        icon = (Sprite)EditorGUILayout.ObjectField("Icon", icon, typeof(Sprite), false);

        price = EditorGUILayout.TextField("Price", price);

        rewarded = EditorGUILayout.Toggle("Rewarded Ad", rewarded);
    }

    void DrawTarget()
    {
        if (type == CreaturesType.Map || type == CreaturesType.Weapon)
            return;

        GUILayout.Label("Target Object", EditorStyles.boldLabel);

        targetObject = (GameObject)EditorGUILayout.ObjectField(
            targetObject, typeof(GameObject), true);
    }

    void CreateCreature()
    {
        CreaturesData data = ScriptableObject.CreateInstance<CreaturesData>();

        string folder = GetFolder(type);

        if (!AssetDatabase.IsValidFolder(folder))
            System.IO.Directory.CreateDirectory(folder);

        string path = folder + creatureName + ".asset";

        AssetDatabase.CreateAsset(data, path);

        SerializedObject so = new SerializedObject(data);

        so.FindProperty("_creatureType").enumValueIndex = (int)type;
        so.FindProperty("_weaponIndex").intValue = weaponIndex;
        so.FindProperty("_creatureName").stringValue = creatureName;
        so.FindProperty("_mapIndex").stringValue = mapIndex;

        var stats = so.FindProperty("_creatureStats");

        stats.FindPropertyRelative("MoveSpeed").floatValue = moveSpeed;
        stats.FindPropertyRelative("AttackPower").intValue = attackPower;

        var ui = so.FindProperty("_creatureUI");

        ui.FindPropertyRelative("CreatureIcon").objectReferenceValue = icon;
        ui.FindPropertyRelative("CreaturePrice").stringValue = price;
        ui.FindPropertyRelative("CreatureName").stringValue = creatureName;
        ui.FindPropertyRelative("CanViewRewardedAd").boolValue = rewarded;

        so.ApplyModifiedProperties();

        repository.CreaturesCollection.Add(data);

        EditorUtility.SetDirty(repository);
        AssetDatabase.SaveAssets();

        if (targetObject != null)
            SetupObject(targetObject);

        Close();
    }

    void SetupObject(GameObject go)
    {
        if (!go.GetComponent<Animator>())
            go.AddComponent<Animator>();

        if (!go.GetComponent<NavMeshAgent>())
            go.AddComponent<NavMeshAgent>();

        if (!go.GetComponent<Rigidbody>())
        {
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 75;
        }

        if (!go.GetComponent<BoxCollider>())
        {
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
        }
    }

    string GetFolder(CreaturesType type)
    {
        switch (type)
        {
            case CreaturesType.Car:
                return "Assets/_DATA/Creature/Car/";
            case CreaturesType.Entity:
                return "Assets/_DATA/Creature/Entity/";
            case CreaturesType.Map:
                return "Assets/_DATA/Creature/Map/";
            case CreaturesType.Weapon:
                return "Assets/_DATA/Creature/Weapon/";
            default:
                return "Assets/_DATA/Creature/";
        }
    }
}