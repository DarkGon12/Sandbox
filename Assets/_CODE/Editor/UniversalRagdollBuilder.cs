using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[AddComponentMenu("Ragdoll/Universal Ragdoll Controller RU")]
public class UniversalRagdollController : MonoBehaviour
{
    [Header("Русский контроллер ragdoll")]
    [Tooltip("Animator, который будет отключаться при включении ragdoll. Можно оставить пустым.")]
    [InspectorName("Animator модели")]
    public Animator animator;

    [Tooltip("Если включено, Animator отключается при активации ragdoll и включается обратно при деактивации.")]
    [InspectorName("Отключать Animator при ragdoll")]
    public bool disableAnimatorWhenRagdoll = true;

    [Tooltip("Если включено, ragdoll включится в Awake().")]
    [InspectorName("Включить ragdoll при старте")]
    public bool enableRagdollOnStart = false;

    [Tooltip("Если включено, коллайдеры ragdoll будут выключены, когда ragdoll не активен.")]
    [InspectorName("Выключать коллайдеры вне ragdoll")]
    public bool disableCollidersWhenInactive = false;

    [Header("Части ragdoll")]
    [InspectorName("Rigidbody частей")]
    public Rigidbody[] bodies = new Rigidbody[0];

    [InspectorName("Collider частей")]
    public Collider[] colliders = new Collider[0];

    private bool _ragdollEnabled;

    private void Awake()
    {
        if (bodies == null || bodies.Length == 0)
            CollectParts();

        SetRagdoll(enableRagdollOnStart);
    }

    [ContextMenu("Собрать части ragdoll")]
    public void CollectParts()
    {
        UniversalRagdollPartRU[] parts = GetComponentsInChildren<UniversalRagdollPartRU>(true);

        bodies = parts
            .Select(p => p.body != null ? p.body : p.GetComponent<Rigidbody>())
            .Where(rb => rb != null)
            .Distinct()
            .ToArray();

        colliders = parts
            .Select(p => p.collider != null ? p.collider : p.GetComponent<Collider>())
            .Where(c => c != null)
            .Distinct()
            .ToArray();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    [ContextMenu("Включить ragdoll")]
    public void EnableRagdoll()
    {
        SetRagdoll(true);
    }

    [ContextMenu("Выключить ragdoll")]
    public void DisableRagdoll()
    {
        SetRagdoll(false);
    }

    public void SetRagdoll(bool enabled)
    {
        _ragdollEnabled = enabled;

        if (bodies != null)
        {
            foreach (Rigidbody rb in bodies)
            {
                if (rb == null) continue;
                rb.isKinematic = !enabled;
                rb.detectCollisions = enabled || !disableCollidersWhenInactive;
            }
        }

        if (colliders != null && disableCollidersWhenInactive)
        {
            foreach (Collider c in colliders)
            {
                if (c == null) continue;
                c.enabled = enabled;
            }
        }

        if (animator != null && disableAnimatorWhenRagdoll)
            animator.enabled = !enabled;
    }

    public bool IsRagdollEnabled()
    {
        return _ragdollEnabled;
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("")]
public class UniversalRagdollPartRU : MonoBehaviour
{
    [Header("Информация о части ragdoll")]
    [InspectorName("Роль")]
    public string role = "Прочая часть";

    [InspectorName("Создано инструментом: Rigidbody")]
    public bool createdRigidbody;

    [InspectorName("Создано инструментом: Collider")]
    public bool createdCollider;

    [InspectorName("Создано инструментом: Joint")]
    public bool createdJoint;

    [InspectorName("Rigidbody")]
    public Rigidbody body;

    [InspectorName("Collider")]
    public Collider collider;

    [InspectorName("Character Joint")]
    public CharacterJoint joint;

    [InspectorName("Соединено с")]
    public Transform connectedTo;
}

#if UNITY_EDITOR
public class UniversalRagdollBuilderRU : EditorWindow
{
    private enum PartRole
    {
        Игнор,
        Корень,
        Корпус,
        Таз,
        Грудь,
        Голова,
        Шея,
        ВерхняяЛапа,
        НижняяЛапа,
        Кисть,
        Бедро,
        Голень,
        Ступня,
        Хвост,
        Щупальце,
        Крыло,
        Колесо,
        Машина,
        ПрочаяЧасть
    }

    private enum ColliderKind
    {
        Авто,
        Капсула,
        Коробка,
        Сфера
    }

    [Serializable]
    private class Part
    {
        public bool use = true;
        public Transform transform;
        public Transform connectedTo;
        public PartRole role = PartRole.ПрочаяЧасть;
        public ColliderKind colliderKind = ColliderKind.Авто;
        public float massWeight = 1f;
        public float radiusMultiplier = 1f;
        public float twistLow = -20f;
        public float twistHigh = 20f;
        public float swing1 = 35f;
        public float swing2 = 25f;
    }

    private GameObject rootObject;
    private readonly List<Part> parts = new List<Part>();
    private Vector2 scroll;

    private bool removePreviousGenerated = true;
    private bool createController = true;
    private bool makeKinematicAfterCreate = true;
    private bool disableAnimatorInController = true;
    private bool reuseExistingColliders = false;
    private bool includeLeafBones = true;
    private bool ignoreHelpers = true;
    private bool forceIncludeSkinnedMeshBones = true;
    private bool includeAllChildTransforms = false;
    private bool includeShortOrZeroLengthBones = true;
    private int maxParts = 256;
    private int lastSkinnedMeshBoneCount = 0;

    private float totalMass = 25f;
    private float capsuleRadiusFactor = 0.22f;
    private float minWorldRadius = 0.015f;
    private float maxWorldRadius = 0.35f;
    private float boxPadding = 1.15f;

    private GUIStyle _titleStyle;
    private GUIStyle _helpBoxStyle;

    [MenuItem("Инструменты/Ragdoll/Универсальный Ragdoll Builder RU")]
    public static void Open()
    {
        UniversalRagdollBuilderRU window = GetWindow<UniversalRagdollBuilderRU>("Ragdoll Builder RU");
        window.minSize = new Vector2(760, 540);
        window.Show();
    }

    private void OnEnable()
    {
        if (rootObject == null && Selection.activeGameObject != null)
            rootObject = Selection.activeGameObject;
    }

    private void OnGUI()
    {
        EnsureStyles();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Универсальный создатель Ragdoll для Generic-аватаров", _titleStyle);
        EditorGUILayout.HelpBox(
            "Инструмент не требует Humanoid Avatar. Он строит ragdoll по иерархии Transform и ключевым словам в именах: body/spine/head/leg/wing/tail/tentacle/wheel и т.п. " +
            "Подходит для животных, монстров, щупалец, машин и нестандартных generic-моделей. После автосоздания проверьте коллайдеры и лимиты суставов в сцене.",
            MessageType.Info);

        EditorGUILayout.Space(6);
        DrawRootBlock();
        EditorGUILayout.Space(8);
        DrawSettingsBlock();
        EditorGUILayout.Space(8);
        DrawActionButtons();
        EditorGUILayout.Space(8);
        DrawPartsList();
    }

    private void EnsureStyles()
    {
        if (_titleStyle == null)
        {
            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                wordWrap = true
            };
        }

        if (_helpBoxStyle == null)
        {
            _helpBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                wordWrap = true
            };
        }
    }

    private void DrawRootBlock()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("1. Объект", EditorStyles.boldLabel);

        rootObject = (GameObject)EditorGUILayout.ObjectField(
            "Корневой объект модели",
            rootObject,
            typeof(GameObject),
            true);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Взять выделенный объект", GUILayout.Height(24)))
        {
            if (Selection.activeGameObject != null)
                rootObject = Selection.activeGameObject;
        }

        if (GUILayout.Button("Сканировать и авто-назначить", GUILayout.Height(24)))
        {
            Scan();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawSettingsBlock()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("2. Настройки генерации", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        removePreviousGenerated = EditorGUILayout.ToggleLeft("Удалять ранее созданный ragdoll этим инструментом", removePreviousGenerated);
        createController = EditorGUILayout.ToggleLeft("Добавить русский контроллер включения/выключения ragdoll", createController);
        makeKinematicAfterCreate = EditorGUILayout.ToggleLeft("После создания оставить Rigidbody в Kinematic", makeKinematicAfterCreate);
        disableAnimatorInController = EditorGUILayout.ToggleLeft("Контроллер будет отключать Animator при ragdoll", disableAnimatorInController);
        reuseExistingColliders = EditorGUILayout.ToggleLeft("Использовать существующие Collider, если они есть", reuseExistingColliders);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        includeLeafBones = EditorGUILayout.ToggleLeft("Включать конечные кости: кисти, лапы, кончики щупалец", includeLeafBones);
        ignoreHelpers = EditorGUILayout.ToggleLeft("Игнорировать helper/IK/target/socket/visual объекты", ignoreHelpers);
        forceIncludeSkinnedMeshBones = EditorGUILayout.ToggleLeft("Всегда добавлять кости из SkinnedMeshRenderer.bones", forceIncludeSkinnedMeshBones);
        includeAllChildTransforms = EditorGUILayout.ToggleLeft("Включать ВСЕ дочерние Transform под корнем", includeAllChildTransforms);
        includeShortOrZeroLengthBones = EditorGUILayout.ToggleLeft("Включать короткие/нулевые кости", includeShortOrZeroLengthBones);
        maxParts = EditorGUILayout.IntSlider("Максимум частей", maxParts, 8, 1024);
        totalMass = EditorGUILayout.FloatField("Общая масса", Mathf.Max(0.1f, totalMass));
        capsuleRadiusFactor = EditorGUILayout.Slider("Толщина капсул от длины", capsuleRadiusFactor, 0.03f, 0.6f);
        minWorldRadius = EditorGUILayout.FloatField("Мин. радиус в мире", Mathf.Max(0.001f, minWorldRadius));
        maxWorldRadius = EditorGUILayout.FloatField("Макс. радиус в мире", Mathf.Max(minWorldRadius, maxWorldRadius));
        boxPadding = EditorGUILayout.Slider("Запас коробок", boxPadding, 1f, 2f);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("3. Действия", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = rootObject != null;
        if (GUILayout.Button("Создать ragdoll", GUILayout.Height(34)))
        {
            if (parts.Count == 0)
                Scan();

            CreateRagdoll();
        }

        if (GUILayout.Button("Удалить созданный ragdoll", GUILayout.Height(34)))
        {
            RemoveGeneratedRagdoll();
        }

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        int activeCount = parts.Count(p => p.use && p.role != PartRole.Игнор && p.transform != null);
        EditorGUILayout.LabelField($"Найдено частей: {parts.Count}. Будет создано/обновлено: {activeCount}.");
        if (lastSkinnedMeshBoneCount > 0)
            EditorGUILayout.LabelField($"Костей из SkinnedMeshRenderer.bones под выбранным корнем: {lastSkinnedMeshBoneCount}.");
        else if (forceIncludeSkinnedMeshBones && rootObject != null)
            EditorGUILayout.LabelField("Кости из SkinnedMeshRenderer.bones рядом с выбранным корнем не найдены.");

        EditorGUILayout.EndVertical();
    }

    private void DrawPartsList()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("4. Части ragdoll — можно править вручную", EditorStyles.boldLabel);

        if (parts.Count == 0)
        {
            EditorGUILayout.LabelField("Список пуст. Нажмите «Сканировать и авто-назначить».", _helpBoxStyle);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("✓", GUILayout.Width(22));
        GUILayout.Label("Transform", GUILayout.MinWidth(180));
        GUILayout.Label("Роль", GUILayout.Width(110));
        GUILayout.Label("Соединить с", GUILayout.MinWidth(160));
        GUILayout.Label("Collider", GUILayout.Width(80));
        GUILayout.Label("Масса", GUILayout.Width(52));
        GUILayout.Label("Радиус", GUILayout.Width(52));
        GUILayout.Label("Лимиты", GUILayout.Width(160));
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(220));

        for (int i = 0; i < parts.Count; i++)
        {
            Part p = parts[i];
            if (p == null) continue;

            EditorGUILayout.BeginHorizontal();
            p.use = EditorGUILayout.Toggle(p.use, GUILayout.Width(22));

            GUI.enabled = false;
            EditorGUILayout.ObjectField(p.transform, typeof(Transform), true, GUILayout.MinWidth(180));
            GUI.enabled = true;

            p.role = (PartRole)EditorGUILayout.EnumPopup(p.role, GUILayout.Width(110));
            p.connectedTo = (Transform)EditorGUILayout.ObjectField(p.connectedTo, typeof(Transform), true, GUILayout.MinWidth(160));
            p.colliderKind = (ColliderKind)EditorGUILayout.EnumPopup(p.colliderKind, GUILayout.Width(80));
            p.massWeight = EditorGUILayout.FloatField(Mathf.Max(0.01f, p.massWeight), GUILayout.Width(52));
            p.radiusMultiplier = EditorGUILayout.FloatField(Mathf.Max(0.05f, p.radiusMultiplier), GUILayout.Width(52));

            EditorGUILayout.BeginVertical(GUILayout.Width(160));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Twist", GUILayout.Width(34));
            p.twistLow = EditorGUILayout.FloatField(p.twistLow, GUILayout.Width(50));
            p.twistHigh = EditorGUILayout.FloatField(p.twistHigh, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Swing", GUILayout.Width(34));
            p.swing1 = EditorGUILayout.FloatField(p.swing1, GUILayout.Width(50));
            p.swing2 = EditorGUILayout.FloatField(p.swing2, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Авто-назначить роли заново"))
        {
            foreach (Part p in parts)
            {
                if (p == null || p.transform == null) continue;
                p.role = GuessRole(p.transform);
                ApplyRoleDefaults(p);
            }

            AutoAssignParents();
        }

        if (GUILayout.Button("Пересчитать соединения"))
        {
            AutoAssignParents();
        }

        if (GUILayout.Button("Отключить прочие части"))
        {
            foreach (Part p in parts)
                if (p != null && p.role == PartRole.ПрочаяЧасть)
                    p.use = false;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void Scan()
    {
        parts.Clear();

        if (rootObject == null)
        {
            EditorUtility.DisplayDialog("Ragdoll Builder", "Сначала выберите корневой объект.", "Ок");
            return;
        }

        Transform root = rootObject.transform;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        HashSet<Transform> skinnedMeshBones = forceIncludeSkinnedMeshBones
            ? CollectSkinnedMeshBones(root)
            : new HashSet<Transform>();
        lastSkinnedMeshBoneCount = skinnedMeshBones.Count;

        List<Part> found = new List<Part>();

        foreach (Transform t in all)
        {
            if (t == null) continue;

            bool isRoot = t == root;
            if (!isRoot && ignoreHelpers && ShouldIgnoreByName(t.name))
                continue;

            bool isSkinnedMeshBone = skinnedMeshBones.Contains(t);

            if (!IsGoodCandidate(root, t, isRoot, isSkinnedMeshBone))
                continue;

            Part part = new Part
            {
                transform = t,
                role = GuessRole(t),
                colliderKind = ColliderKind.Авто
            };

            ApplyRoleDefaults(part);
            found.Add(part);
        }

        if (found.Count == 0)
        {
            Part fallback = new Part
            {
                transform = root,
                role = GuessRole(root),
                colliderKind = ColliderKind.Авто
            };
            ApplyRoleDefaults(fallback);
            found.Add(fallback);
        }

        found = found
            .OrderBy(p => GetDepth(root, p.transform))
            .ThenBy(p => p.transform.GetSiblingIndex())
            .ToList();

        if (found.Count > maxParts)
        {
            found = found
                .OrderByDescending(p => CandidatePriority(p.role))
                .ThenBy(p => GetDepth(root, p.transform))
                .Take(maxParts)
                .OrderBy(p => GetDepth(root, p.transform))
                .ThenBy(p => p.transform.GetSiblingIndex())
                .ToList();
        }

        parts.AddRange(found);
        AutoAssignParents();

        Repaint();
    }

    private bool IsGoodCandidate(Transform root, Transform t, bool isRoot, bool isSkinnedMeshBone)
    {
        if (t == null) return false;

        bool hasDirectRenderer = t.GetComponent<Renderer>() != null;
        PartRole role = GuessRole(t);

        if (role == PartRole.Игнор)
            return false;

        if (isRoot)
        {
            if (hasDirectRenderer) return true;
            if (root.GetComponentsInChildren<Transform>(true).Length <= 2) return true;
            return false;
        }

        // Максимальный режим: нужен для моделей, где все кости названы одинаково
        // вроде "Кость.001", "Bone.002" или имеют почти нулевую длину.
        if (includeAllChildTransforms)
            return true;

        // Главный фикс для Generic-аватаров: берем не только "умные" имена,
        // а все Transform, которые реально используются SkinnedMeshRenderer как bones.
        if (isSkinnedMeshBone)
        {
            if (includeShortOrZeroLengthBones)
                return true;

            Transform child = FirstUsefulChild(t);
            if (child != null && Vector3.Distance(t.position, child.position) > minWorldRadius * 2f)
                return true;

            if (t.parent != null && Vector3.Distance(t.position, t.parent.position) > minWorldRadius * 2f)
                return true;
        }

        if (hasDirectRenderer)
            return true;

        if (role != PartRole.ПрочаяЧасть)
            return true;

        int childCount = CountUsefulChildren(t);
        if (childCount > 1)
            return true;

        if (childCount == 1)
        {
            Transform child = FirstUsefulChild(t);
            if (child != null)
            {
                float dist = Vector3.Distance(t.position, child.position);
                if (includeShortOrZeroLengthBones || dist > minWorldRadius * 2f)
                    return true;
            }
        }

        if (includeLeafBones && t.childCount == 0 && t.parent != null)
        {
            float dist = Vector3.Distance(t.position, t.parent.position);
            return includeShortOrZeroLengthBones || dist > minWorldRadius * 2f;
        }

        return false;
    }


    private HashSet<Transform> CollectSkinnedMeshBones(Transform boneRoot)
    {
        HashSet<Transform> result = new HashSet<Transform>();
        if (boneRoot == null)
            return result;

        // Ищем SkinnedMeshRenderer не только внутри выбранной арматуры,
        // но и рядом: часто mesh лежит соседним объектом рядом с Armature.
        AddSkinnedMeshBonesFromSearchRoot(boneRoot, boneRoot, result);

        if (boneRoot.parent != null)
            AddSkinnedMeshBonesFromSearchRoot(boneRoot.parent, boneRoot, result);

        Transform top = boneRoot.root;
        if (top != null && top != boneRoot && top != boneRoot.parent)
            AddSkinnedMeshBonesFromSearchRoot(top, boneRoot, result);

        return result;
    }

    private void AddSkinnedMeshBonesFromSearchRoot(Transform searchRoot, Transform boneRoot, HashSet<Transform> result)
    {
        if (searchRoot == null || boneRoot == null || result == null)
            return;

        SkinnedMeshRenderer[] renderers = searchRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (renderer.rootBone != null && IsSameOrChildOf(renderer.rootBone, boneRoot))
                result.Add(renderer.rootBone);

            Transform[] rendererBones = renderer.bones;
            if (rendererBones == null)
                continue;

            foreach (Transform bone in rendererBones)
            {
                if (bone == null)
                    continue;

                if (IsSameOrChildOf(bone, boneRoot))
                    result.Add(bone);
            }
        }
    }

    private bool IsSameOrChildOf(Transform value, Transform possibleParent)
    {
        if (value == null || possibleParent == null)
            return false;

        Transform cursor = value;
        while (cursor != null)
        {
            if (cursor == possibleParent)
                return true;

            cursor = cursor.parent;
        }

        return false;
    }

    private void AutoAssignParents()
    {
        HashSet<Transform> used = new HashSet<Transform>(parts
            .Where(p => p != null && p.use && p.role != PartRole.Игнор && p.transform != null)
            .Select(p => p.transform));

        foreach (Part p in parts)
        {
            if (p == null || p.transform == null)
                continue;

            Transform ancestor = p.transform.parent;
            p.connectedTo = null;

            while (ancestor != null)
            {
                if (used.Contains(ancestor))
                {
                    p.connectedTo = ancestor;
                    break;
                }

                if (rootObject != null && ancestor == rootObject.transform)
                    break;

                ancestor = ancestor.parent;
            }
        }
    }

    private void ApplyRoleDefaults(Part p)
    {
        if (p == null) return;

        switch (p.role)
        {
            case PartRole.Голова:
                p.massWeight = 0.8f;
                p.radiusMultiplier = 1.15f;
                p.swing1 = 25f;
                p.swing2 = 20f;
                p.twistLow = -25f;
                p.twistHigh = 25f;
                p.colliderKind = ColliderKind.Сфера;
                break;

            case PartRole.Шея:
                p.massWeight = 0.35f;
                p.radiusMultiplier = 0.8f;
                p.swing1 = 25f;
                p.swing2 = 25f;
                p.twistLow = -20f;
                p.twistHigh = 20f;
                break;

            case PartRole.Корпус:
            case PartRole.Таз:
            case PartRole.Грудь:
            case PartRole.Машина:
            case PartRole.Корень:
                p.massWeight = p.role == PartRole.Машина ? 5f : 3f;
                p.radiusMultiplier = 1.35f;
                p.swing1 = 15f;
                p.swing2 = 15f;
                p.twistLow = -10f;
                p.twistHigh = 10f;
                p.colliderKind = ColliderKind.Коробка;
                break;

            case PartRole.Колесо:
                p.massWeight = 0.7f;
                p.radiusMultiplier = 1.0f;
                p.swing1 = 45f;
                p.swing2 = 45f;
                p.twistLow = -90f;
                p.twistHigh = 90f;
                p.colliderKind = ColliderKind.Сфера;
                break;

            case PartRole.Хвост:
            case PartRole.Щупальце:
                p.massWeight = 0.45f;
                p.radiusMultiplier = 0.75f;
                p.swing1 = 45f;
                p.swing2 = 45f;
                p.twistLow = -35f;
                p.twistHigh = 35f;
                p.colliderKind = ColliderKind.Капсула;
                break;

            case PartRole.Крыло:
                p.massWeight = 0.45f;
                p.radiusMultiplier = 0.55f;
                p.swing1 = 50f;
                p.swing2 = 35f;
                p.twistLow = -35f;
                p.twistHigh = 35f;
                p.colliderKind = ColliderKind.Капсула;
                break;

            case PartRole.Кисть:
            case PartRole.Ступня:
                p.massWeight = 0.35f;
                p.radiusMultiplier = 0.9f;
                p.swing1 = 30f;
                p.swing2 = 25f;
                p.twistLow = -20f;
                p.twistHigh = 20f;
                p.colliderKind = ColliderKind.Капсула;
                break;

            case PartRole.ВерхняяЛапа:
            case PartRole.НижняяЛапа:
            case PartRole.Бедро:
            case PartRole.Голень:
                p.massWeight = 0.75f;
                p.radiusMultiplier = 0.85f;
                p.swing1 = 35f;
                p.swing2 = 25f;
                p.twistLow = -25f;
                p.twistHigh = 25f;
                p.colliderKind = ColliderKind.Капсула;
                break;

            default:
                p.massWeight = 0.6f;
                p.radiusMultiplier = 0.8f;
                p.swing1 = 35f;
                p.swing2 = 25f;
                p.twistLow = -20f;
                p.twistHigh = 20f;
                p.colliderKind = ColliderKind.Капсула;
                break;
        }
    }

    private void CreateRagdoll()
    {
        if (rootObject == null)
        {
            EditorUtility.DisplayDialog("Ragdoll Builder", "Сначала выберите корневой объект.", "Ок");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Создать универсальный ragdoll");

        if (removePreviousGenerated)
            RemoveGeneratedRagdoll(false);

        List<Part> activeParts = parts
            .Where(p => p != null && p.use && p.role != PartRole.Игнор && p.transform != null)
            .ToList();

        if (activeParts.Count == 0)
        {
            EditorUtility.DisplayDialog("Ragdoll Builder", "Нет активных частей для создания.", "Ок");
            return;
        }

        Dictionary<Transform, Rigidbody> bodyMap = new Dictionary<Transform, Rigidbody>();
        Dictionary<Transform, Collider> colliderMap = new Dictionary<Transform, Collider>();

        float weightSum = activeParts.Sum(p => Mathf.Max(0.01f, p.massWeight));
        if (weightSum <= 0f) weightSum = 1f;

        foreach (Part p in activeParts)
        {
            GameObject go = p.transform.gameObject;
            UniversalRagdollPartRU marker = go.GetComponent<UniversalRagdollPartRU>();
            if (marker == null)
            {
                marker = Undo.AddComponent<UniversalRagdollPartRU>(go);
            }
            else
            {
                Undo.RecordObject(marker, "Обновить marker ragdoll");
            }

            Rigidbody rb = go.GetComponent<Rigidbody>();
            bool createdRb = false;

            if (rb == null)
            {
                rb = Undo.AddComponent<Rigidbody>(go);
                createdRb = true;
            }
            else
            {
                Undo.RecordObject(rb, "Обновить Rigidbody ragdoll");
            }

            rb.mass = Mathf.Max(0.01f, totalMass * Mathf.Max(0.01f, p.massWeight) / weightSum);
            rb.isKinematic = makeKinematicAfterCreate;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.linearDamping = 0.03f;
            rb.angularDamping = 0.12f;

            Collider collider = null;
            bool createdCollider = false;

            if (reuseExistingColliders)
                collider = go.GetComponents<Collider>().FirstOrDefault(c => c != null && !c.isTrigger);

            if (collider == null)
            {
                collider = CreateColliderForPart(p);
                createdCollider = true;
            }
            else
            {
                Undo.RecordObject(collider, "Обновить Collider ragdoll");
            }

            marker.role = p.role.ToString();
            marker.createdRigidbody = createdRb;
            marker.createdCollider = createdCollider;
            marker.createdJoint = false;
            marker.body = rb;
            marker.collider = collider;
            marker.joint = null;
            marker.connectedTo = p.connectedTo;

            EditorUtility.SetDirty(marker);
            EditorUtility.SetDirty(rb);
            if (collider != null) EditorUtility.SetDirty(collider);

            bodyMap[p.transform] = rb;
            if (collider != null) colliderMap[p.transform] = collider;
        }

        foreach (Part p in activeParts)
        {
            if (p.connectedTo == null)
                continue;

            if (!bodyMap.TryGetValue(p.transform, out Rigidbody rb))
                continue;

            Rigidbody connectedBody = null;
            Transform cursor = p.connectedTo;
            while (cursor != null && connectedBody == null)
            {
                bodyMap.TryGetValue(cursor, out connectedBody);
                cursor = cursor.parent;
            }

            if (connectedBody == null || connectedBody == rb)
                continue;

            GameObject go = p.transform.gameObject;
            CharacterJoint joint = go.GetComponent<CharacterJoint>();
            bool createdJoint = false;

            if (joint == null)
            {
                joint = Undo.AddComponent<CharacterJoint>(go);
                createdJoint = true;
            }
            else
            {
                Undo.RecordObject(joint, "Обновить CharacterJoint ragdoll");
            }

            ConfigureCharacterJoint(joint, p, connectedBody);

            UniversalRagdollPartRU marker = go.GetComponent<UniversalRagdollPartRU>();
            if (marker != null)
            {
                Undo.RecordObject(marker, "Обновить marker joint");
                marker.createdJoint = createdJoint;
                marker.joint = joint;
                marker.connectedTo = connectedBody.transform;
                EditorUtility.SetDirty(marker);
            }

            EditorUtility.SetDirty(joint);
        }

        if (createController)
            CreateOrUpdateController(bodyMap.Values.ToArray(), colliderMap.Values.ToArray());

        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeObject = rootObject;
        EditorUtility.DisplayDialog(
            "Ragdoll Builder",
            "Готово. Ragdoll создан. Проверьте коллайдеры и лимиты суставов в Scene View.",
            "Ок");
    }

    private Collider CreateColliderForPart(Part p)
    {
        ColliderKind kind = p.colliderKind;
        if (kind == ColliderKind.Авто)
            kind = AutoColliderKind(p);

        switch (kind)
        {
            case ColliderKind.Коробка:
                return CreateBoxCollider(p);

            case ColliderKind.Сфера:
                return CreateSphereCollider(p);

            default:
                return CreateCapsuleCollider(p);
        }
    }

    private ColliderKind AutoColliderKind(Part p)
    {
        switch (p.role)
        {
            case PartRole.Голова:
            case PartRole.Колесо:
                return ColliderKind.Сфера;

            case PartRole.Корпус:
            case PartRole.Таз:
            case PartRole.Грудь:
            case PartRole.Машина:
            case PartRole.Корень:
                return ColliderKind.Коробка;

            default:
                return ColliderKind.Капсула;
        }
    }

    private CapsuleCollider CreateCapsuleCollider(Part p)
    {
        CapsuleCollider capsule = Undo.AddComponent<CapsuleCollider>(p.transform.gameObject);

        Vector3 worldA;
        Vector3 worldB;
        GetBestSegment(p, out worldA, out worldB);

        Transform t = p.transform;
        Vector3 localA = t.InverseTransformPoint(worldA);
        Vector3 localB = t.InverseTransformPoint(worldB);
        Vector3 localDelta = localB - localA;

        int direction = DominantAxis(localDelta);
        float axisScale = GetWorldScaleOnAxis(t, direction);
        float worldLength = Vector3.Distance(worldA, worldB);
        if (worldLength < minWorldRadius * 2f)
            worldLength = minWorldRadius * 2f;

        float worldRadius = Mathf.Clamp(worldLength * capsuleRadiusFactor * p.radiusMultiplier, minWorldRadius, maxWorldRadius);
        float localRadius = Mathf.Max(0.001f, worldRadius / Mathf.Max(0.001f, AveragePerpendicularScale(t, direction)));
        float localHeight = Mathf.Max(localRadius * 2.1f, worldLength / Mathf.Max(0.001f, axisScale));

        capsule.direction = direction;
        capsule.center = (localA + localB) * 0.5f;
        capsule.radius = localRadius;
        capsule.height = localHeight;

        return capsule;
    }

    private BoxCollider CreateBoxCollider(Part p)
    {
        BoxCollider box = Undo.AddComponent<BoxCollider>(p.transform.gameObject);

        Bounds b;
        if (!TryLocalBounds(p, out b))
        {
            Vector3 worldA;
            Vector3 worldB;
            GetBestSegment(p, out worldA, out worldB);
            Vector3 localA = p.transform.InverseTransformPoint(worldA);
            Vector3 localB = p.transform.InverseTransformPoint(worldB);
            b = new Bounds((localA + localB) * 0.5f, AbsVector(localB - localA));
        }

        Vector3 minSize = WorldSizeToLocal(p.transform, new Vector3(minWorldRadius * 2f, minWorldRadius * 2f, minWorldRadius * 2f));
        Vector3 size = MaxVector(b.size * boxPadding, minSize);

        box.center = b.center;
        box.size = size;

        return box;
    }

    private SphereCollider CreateSphereCollider(Part p)
    {
        SphereCollider sphere = Undo.AddComponent<SphereCollider>(p.transform.gameObject);

        Bounds b;
        if (!TryLocalBounds(p, out b))
        {
            Vector3 worldA;
            Vector3 worldB;
            GetBestSegment(p, out worldA, out worldB);
            Vector3 localA = p.transform.InverseTransformPoint(worldA);
            Vector3 localB = p.transform.InverseTransformPoint(worldB);
            b = new Bounds((localA + localB) * 0.5f, AbsVector(localB - localA));
        }

        float localMaxExtent = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z));
        float scale = MaxLossyScale(p.transform);
        float minLocal = minWorldRadius / Mathf.Max(0.001f, scale);
        float maxLocal = maxWorldRadius / Mathf.Max(0.001f, scale);

        sphere.center = b.center;
        sphere.radius = Mathf.Clamp(localMaxExtent * p.radiusMultiplier, minLocal, maxLocal);

        return sphere;
    }

    private void ConfigureCharacterJoint(CharacterJoint joint, Part p, Rigidbody connectedBody)
    {
        joint.connectedBody = connectedBody;
        joint.autoConfigureConnectedAnchor = true;
        joint.enableCollision = false;
        joint.enableProjection = true;
        joint.projectionDistance = 0.1f;
        joint.projectionAngle = 10f;

        Vector3 axisWorld = (p.transform.position - connectedBody.transform.position);
        if (axisWorld.sqrMagnitude < 0.000001f)
        {
            Vector3 a;
            Vector3 b;
            GetBestSegment(p, out a, out b);
            axisWorld = b - a;
        }

        if (axisWorld.sqrMagnitude < 0.000001f)
            axisWorld = p.transform.forward;

        Vector3 axisLocal = p.transform.InverseTransformDirection(axisWorld.normalized);
        if (axisLocal.sqrMagnitude < 0.000001f)
            axisLocal = Vector3.right;
        axisLocal.Normalize();

        Vector3 swingLocal = Vector3.Cross(axisLocal, Vector3.up);
        if (swingLocal.sqrMagnitude < 0.000001f)
            swingLocal = Vector3.Cross(axisLocal, Vector3.forward);
        if (swingLocal.sqrMagnitude < 0.000001f)
            swingLocal = Vector3.forward;
        swingLocal.Normalize();

        joint.axis = axisLocal;
        joint.swingAxis = swingLocal;
        joint.anchor = Vector3.zero;

        SoftJointLimit low = new SoftJointLimit { limit = Mathf.Min(p.twistLow, p.twistHigh) };
        SoftJointLimit high = new SoftJointLimit { limit = Mathf.Max(p.twistLow, p.twistHigh) };
        SoftJointLimit swing1 = new SoftJointLimit { limit = Mathf.Max(0f, p.swing1) };
        SoftJointLimit swing2 = new SoftJointLimit { limit = Mathf.Max(0f, p.swing2) };

        joint.lowTwistLimit = low;
        joint.highTwistLimit = high;
        joint.swing1Limit = swing1;
        joint.swing2Limit = swing2;
    }

    private void CreateOrUpdateController(Rigidbody[] bodies, Collider[] colliders)
    {
        UniversalRagdollController controller = rootObject.GetComponent<UniversalRagdollController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<UniversalRagdollController>(rootObject);
        }
        else
        {
            Undo.RecordObject(controller, "Обновить UniversalRagdollControllerRU");
        }

        controller.animator = rootObject.GetComponentInChildren<Animator>(true);
        controller.disableAnimatorWhenRagdoll = disableAnimatorInController;
        controller.enableRagdollOnStart = !makeKinematicAfterCreate;
        controller.disableCollidersWhenInactive = false;
        controller.bodies = bodies.Where(rb => rb != null).Distinct().ToArray();
        controller.colliders = colliders.Where(c => c != null).Distinct().ToArray();

        EditorUtility.SetDirty(controller);
    }

    private void RemoveGeneratedRagdoll(bool showDialog = true)
    {
        if (rootObject == null)
        {
            if (showDialog)
                EditorUtility.DisplayDialog("Ragdoll Builder", "Сначала выберите корневой объект.", "Ок");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Удалить созданный ragdoll");

        UniversalRagdollPartRU[] markers = rootObject.GetComponentsInChildren<UniversalRagdollPartRU>(true);

        foreach (UniversalRagdollPartRU marker in markers)
        {
            if (marker == null) continue;

            if (marker.createdJoint && marker.joint != null)
                Undo.DestroyObjectImmediate(marker.joint);

            if (marker.createdCollider && marker.collider != null)
                Undo.DestroyObjectImmediate(marker.collider);

            if (marker.createdRigidbody && marker.body != null)
                Undo.DestroyObjectImmediate(marker.body);

            Undo.DestroyObjectImmediate(marker);
        }

        UniversalRagdollController controller = rootObject.GetComponent<UniversalRagdollController>();
        if (controller != null)
            Undo.DestroyObjectImmediate(controller);

        Undo.CollapseUndoOperations(undoGroup);

        if (showDialog)
            EditorUtility.DisplayDialog("Ragdoll Builder", "Созданные этим инструментом компоненты удалены.", "Ок");
    }

    private void GetBestSegment(Part p, out Vector3 worldA, out Vector3 worldB)
    {
        Transform t = p.transform;
        worldA = t.position;
        worldB = t.position + t.forward * Mathf.Max(minWorldRadius * 4f, 0.1f);

        Transform bestChild = null;
        float bestDistance = 0f;

        foreach (Transform child in t)
        {
            if (child == null) continue;
            if (ignoreHelpers && ShouldIgnoreByName(child.name)) continue;

            float dist = Vector3.Distance(t.position, child.position);
            if (dist > bestDistance)
            {
                bestDistance = dist;
                bestChild = child;
            }
        }

        if (bestChild != null && bestDistance > minWorldRadius * 2f)
        {
            worldA = t.position;
            worldB = bestChild.position;
            return;
        }

        if (p.connectedTo != null)
        {
            float dist = Vector3.Distance(p.connectedTo.position, t.position);
            if (dist > minWorldRadius * 2f)
            {
                worldA = p.connectedTo.position;
                worldB = t.position;
                return;
            }
        }

        Bounds rb;
        if (TryWorldRendererBounds(t, out rb))
        {
            Vector3 ext = rb.extents;
            if (ext.x >= ext.y && ext.x >= ext.z)
            {
                worldA = rb.center - t.right * ext.x;
                worldB = rb.center + t.right * ext.x;
            }
            else if (ext.y >= ext.x && ext.y >= ext.z)
            {
                worldA = rb.center - t.up * ext.y;
                worldB = rb.center + t.up * ext.y;
            }
            else
            {
                worldA = rb.center - t.forward * ext.z;
                worldB = rb.center + t.forward * ext.z;
            }

            if (Vector3.Distance(worldA, worldB) > minWorldRadius * 2f)
                return;
        }
    }

    private bool TryLocalBounds(Part p, out Bounds bounds)
    {
        Transform t = p.transform;
        List<Vector3> localPoints = new List<Vector3>();

        Renderer directRenderer = t.GetComponent<Renderer>();
        if (directRenderer != null)
            AddBoundsCorners(localPoints, t, directRenderer.bounds);

        foreach (Transform child in t)
        {
            if (child == null) continue;
            if (ignoreHelpers && ShouldIgnoreByName(child.name)) continue;
            localPoints.Add(t.InverseTransformPoint(child.position));
        }

        if (p.connectedTo != null)
            localPoints.Add(t.InverseTransformPoint(p.connectedTo.position));

        if (localPoints.Count == 0)
        {
            bounds = new Bounds(Vector3.zero, Vector3.zero);
            return false;
        }

        bounds = new Bounds(localPoints[0], Vector3.zero);
        for (int i = 1; i < localPoints.Count; i++)
            bounds.Encapsulate(localPoints[i]);

        if (bounds.size.sqrMagnitude < 0.000001f)
            bounds.Expand(WorldSizeToLocal(t, Vector3.one * minWorldRadius * 2f).x);

        return true;
    }

    private bool TryWorldRendererBounds(Transform t, out Bounds bounds)
    {
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>(true)
            .Where(r => r != null && r.enabled)
            .ToArray();

        if (renderers.Length == 0)
        {
            bounds = new Bounds(t.position, Vector3.zero);
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return true;
    }

    private void AddBoundsCorners(List<Vector3> localPoints, Transform owner, Bounds worldBounds)
    {
        Vector3 c = worldBounds.center;
        Vector3 e = worldBounds.extents;

        localPoints.Add(owner.InverseTransformPoint(c + new Vector3(e.x, e.y, e.z)));
        localPoints.Add(owner.InverseTransformPoint(c + new Vector3(e.x, e.y, -e.z)));
        localPoints.Add(owner.InverseTransformPoint(c + new Vector3(e.x, -e.y, e.z)));
        localPoints.Add(owner.InverseTransformPoint(c + new Vector3(e.x, -e.y, -e.z)));
        localPoints.Add(owner.InverseTransformPoint(c + new Vector3(-e.x, e.y, e.z)));
        localPoints.Add(owner.InverseTransformPoint(c + new Vector3(-e.x, e.y, -e.z)));
        localPoints.Add(owner.InverseTransformPoint(c + new Vector3(-e.x, -e.y, e.z)));
        localPoints.Add(owner.InverseTransformPoint(c + new Vector3(-e.x, -e.y, -e.z)));
    }

    private PartRole GuessRole(Transform t)
    {
        if (t == null) return PartRole.Игнор;

        string n = NormalizeName(t.name);

        if (ignoreHelpers && ShouldIgnoreByName(n))
            return PartRole.Игнор;

        if (ContainsAny(n, "tentacle", "tendril", "vine", "whip", "щуп", "усик"))
            return PartRole.Щупальце;

        if (ContainsAny(n, "tail", "хвост"))
            return PartRole.Хвост;

        if (ContainsAny(n, "wing", "крыл"))
            return PartRole.Крыло;

        if (ContainsAny(n, "wheel", "tire", "tyre", "колес", "шина"))
            return PartRole.Колесо;

        if (ContainsAny(n, "chassis", "vehicle", "car", "truck", "машин", "авто"))
            return PartRole.Машина;

        if (ContainsAny(n, "pelvis", "hips", "hip", "таз"))
            return PartRole.Таз;

        if (ContainsAny(n, "chest", "rib", "spine2", "spine_2", "груд"))
            return PartRole.Грудь;

        if (ContainsAny(n, "spine", "torso", "body", "abdomen", "корпус", "тело", "брюш"))
            return PartRole.Корпус;

        if (ContainsAny(n, "head", "skull", "голов"))
            return PartRole.Голова;

        if (ContainsAny(n, "neck", "шея", "шеи"))
            return PartRole.Шея;

        if (ContainsAny(n, "hand", "wrist", "claw", "finger", "кист", "ладон", "палец", "клеш"))
            return PartRole.Кисть;

        if (ContainsAny(n, "foot", "feet", "paw", "toe", "hoof", "ankle", "ступ", "лапа", "копыт"))
            return PartRole.Ступня;

        if (ContainsAny(n, "lowerarm", "forearm", "elbow", "нижнрук", "предплеч", "локт"))
            return PartRole.НижняяЛапа;

        if (ContainsAny(n, "upperarm", "shoulder", "arm", "верхнрук", "плеч"))
            return PartRole.ВерхняяЛапа;

        if (ContainsAny(n, "calf", "shin", "lowerleg", "knee", "голен", "колен"))
            return PartRole.Голень;

        if (ContainsAny(n, "thigh", "upperleg", "leg", "бедр"))
            return PartRole.Бедро;

        if (ContainsAny(n, "root", "armature", "skeleton", "rig", "корень", "арматур", "скелет"))
            return PartRole.Корень;

        if (ContainsAny(n, "bone", "joint", "jnt", "кость", "kost"))
            return PartRole.ПрочаяЧасть;

        return PartRole.ПрочаяЧасть;
    }

    private bool ShouldIgnoreByName(string name)
    {
        string n = NormalizeName(name);

        if (ContainsAny(n,
            "ik", "fk", "target", "ctrl", "control", "helper", "socket", "attach",
            "weapon", "camera", "mesh", "visual", "renderer",
            "effector", "constraint", "locator", "dummy", "gizmo", "marker"))
            return true;

        // End/Nub часто являются настоящими кончиками костей.
        // Поэтому игнорируем их только если пользователь сам выключил конечные кости.
        if (!includeLeafBones && ContainsAny(n, "end", "nub", "конец"))
            return true;

        return false;
    }

    private string NormalizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .ToLowerInvariant()
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "")
            .Replace(".", "");
    }

    private bool ContainsAny(string value, params string[] tokens)
    {
        if (string.IsNullOrEmpty(value)) return false;

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            if (string.IsNullOrEmpty(token)) continue;

            if (value.Contains(token.ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "").Replace(".", "")))
                return true;
        }

        return false;
    }

    private int CountUsefulChildren(Transform t)
    {
        int count = 0;
        foreach (Transform child in t)
        {
            if (child == null) continue;
            if (ignoreHelpers && ShouldIgnoreByName(child.name)) continue;
            count++;
        }

        return count;
    }

    private Transform FirstUsefulChild(Transform t)
    {
        foreach (Transform child in t)
        {
            if (child == null) continue;
            if (ignoreHelpers && ShouldIgnoreByName(child.name)) continue;
            return child;
        }

        return null;
    }

    private int CandidatePriority(PartRole role)
    {
        switch (role)
        {
            case PartRole.Корень:
            case PartRole.Корпус:
            case PartRole.Таз:
            case PartRole.Грудь:
            case PartRole.Голова:
            case PartRole.Машина:
                return 100;

            case PartRole.Шея:
            case PartRole.ВерхняяЛапа:
            case PartRole.НижняяЛапа:
            case PartRole.Бедро:
            case PartRole.Голень:
            case PartRole.Ступня:
            case PartRole.Кисть:
            case PartRole.Крыло:
            case PartRole.Колесо:
                return 80;

            case PartRole.Хвост:
            case PartRole.Щупальце:
                return 70;

            case PartRole.ПрочаяЧасть:
                return 20;

            default:
                return 0;
        }
    }

    private int GetDepth(Transform root, Transform t)
    {
        if (root == null || t == null) return 0;

        int depth = 0;
        Transform cursor = t;
        while (cursor != null && cursor != root)
        {
            depth++;
            cursor = cursor.parent;
        }

        return depth;
    }

    private int DominantAxis(Vector3 v)
    {
        Vector3 a = AbsVector(v);
        if (a.x >= a.y && a.x >= a.z) return 0;
        if (a.y >= a.x && a.y >= a.z) return 1;
        return 2;
    }

    private Vector3 AbsVector(Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    private Vector3 MaxVector(Vector3 a, Vector3 b)
    {
        return new Vector3(
            Mathf.Max(a.x, b.x),
            Mathf.Max(a.y, b.y),
            Mathf.Max(a.z, b.z));
    }

    private Vector3 WorldSizeToLocal(Transform t, Vector3 worldSize)
    {
        Vector3 s = t.lossyScale;
        return new Vector3(
            worldSize.x / Mathf.Max(0.001f, Mathf.Abs(s.x)),
            worldSize.y / Mathf.Max(0.001f, Mathf.Abs(s.y)),
            worldSize.z / Mathf.Max(0.001f, Mathf.Abs(s.z)));
    }

    private float GetWorldScaleOnAxis(Transform t, int axis)
    {
        Vector3 s = t.lossyScale;
        switch (axis)
        {
            case 0: return Mathf.Abs(s.x);
            case 1: return Mathf.Abs(s.y);
            default: return Mathf.Abs(s.z);
        }
    }

    private float AveragePerpendicularScale(Transform t, int axis)
    {
        Vector3 s = AbsVector(t.lossyScale);
        switch (axis)
        {
            case 0: return (s.y + s.z) * 0.5f;
            case 1: return (s.x + s.z) * 0.5f;
            default: return (s.x + s.y) * 0.5f;
        }
    }

    private float MaxLossyScale(Transform t)
    {
        Vector3 s = AbsVector(t.lossyScale);
        return Mathf.Max(s.x, Mathf.Max(s.y, s.z));
    }
}
#endif
