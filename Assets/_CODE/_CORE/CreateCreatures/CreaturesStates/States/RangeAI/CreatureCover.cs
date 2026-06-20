using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Creatures.Navigation
{
    public enum CreatureCoverStance
    {
        Standing = 0,
        Crouching = 1
    }

    public enum CreatureCoverSide
    {
        Left = -1,
        Right = 1
    }

    [Serializable]
    public sealed class CreatureCoverSlot
    {
        [SerializeField] private Transform _standPoint;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private CreatureCoverStance _stance = CreatureCoverStance.Standing;
        [SerializeField] private CreatureCoverSide _side = CreatureCoverSide.Right;
        [SerializeField] private float _priority;

        [NonSerialized] private CreatureRangedCombatContext _reservedBy;

        public Transform StandPoint => _standPoint;
        public Transform FirePoint => _firePoint;
        public CreatureCoverStance Stance => _stance;
        public CreatureCoverSide Side => _side;
        public float Priority => _priority;

        public bool HasStandPoint => _standPoint != null;
        public bool HasFirePoint => _firePoint != null;
        public Vector3 StandPosition => _standPoint != null ? _standPoint.position : Vector3.zero;
        public Vector3 FirePosition => _firePoint != null ? _firePoint.position : StandPosition + Vector3.up * 1.35f;

        public CreatureCoverSlot()
        {
        }

        public CreatureCoverSlot(
            Transform standPoint,
            Transform firePoint,
            CreatureCoverStance stance,
            CreatureCoverSide side,
            float priority = 0f)
        {
            _standPoint = standPoint;
            _firePoint = firePoint;
            _stance = stance;
            _side = side;
            _priority = priority;
        }

        public bool IsFreeFor(CreatureRangedCombatContext requester)
        {
            return _reservedBy == null || _reservedBy == requester;
        }

        public void Reserve(CreatureRangedCombatContext requester)
        {
            _reservedBy = requester;
        }

        public void Release(CreatureRangedCombatContext requester)
        {
            if (_reservedBy == requester)
                _reservedBy = null;
        }

        public void ForceRelease()
        {
            _reservedBy = null;
        }
    }

    public class CreatureCover : MonoBehaviour
    {
        private static readonly List<CreatureCover> _activeCovers = new List<CreatureCover>(128);

        [SerializeField] private Transform _center;
        [SerializeField] private List<CreatureCoverSlot> _slots = new List<CreatureCoverSlot>();
        [SerializeField] private bool _autoGenerateSlotsWhenEmpty = true;
        [SerializeField] private float _autoStandOffset = 0.75f;
        [SerializeField] private float _autoFireHeight = 1.35f;
        [SerializeField] private float _autoFireSideOffset = 0.25f;

        public static IReadOnlyList<CreatureCover> ActiveCovers => _activeCovers;
        public Transform Center => _center != null ? _center : transform;
        public IReadOnlyList<CreatureCoverSlot> Slots => _slots;

        private void Awake()
        {
            TryGenerateAutoSlots();
        }

        private void OnEnable()
        {
            if (!_activeCovers.Contains(this))
                _activeCovers.Add(this);
        }

        private void OnDisable()
        {
            _activeCovers.Remove(this);

            for (int i = 0; i < _slots.Count; i++)
                _slots[i]?.ForceRelease();
        }

        public bool IsSlotBehindEnemy(CreatureCoverSlot slot, Vector3 enemyPosition, float minDot)
        {
            if (slot == null || !slot.HasStandPoint)
                return false;

            Vector3 awayFromEnemy = Flatten(Center.position - enemyPosition);
            Vector3 slotDirection = Flatten(slot.StandPosition - Center.position);

            if (awayFromEnemy.sqrMagnitude < 0.001f || slotDirection.sqrMagnitude < 0.001f)
                return true;

            awayFromEnemy.Normalize();
            slotDirection.Normalize();

            return Vector3.Dot(slotDirection, awayFromEnemy) >= minDot;
        }

        public static bool TryFindBestSlot(
            Vector3 npcPosition,
            Vector3 enemyPosition,
            CreatureRangedCombatSettings settings,
            CreatureRangedCombatContext requester,
            out CreatureCover bestCover,
            out CreatureCoverSlot bestSlot,
            out Vector3 bestNavMeshPosition)
        {
            bestCover = null;
            bestSlot = null;
            bestNavMeshPosition = Vector3.zero;

            if (settings == null)
                return false;

            float bestScore = float.MaxValue;
            float searchRadiusSqr = settings.CoverSearchRadius * settings.CoverSearchRadius;
            NavMeshPath path = new NavMeshPath();

            for (int i = 0; i < _activeCovers.Count; i++)
            {
                CreatureCover cover = _activeCovers[i];
                if (cover == null || !cover.isActiveAndEnabled)
                    continue;

                IReadOnlyList<CreatureCoverSlot> slots = cover.Slots;

                for (int j = 0; j < slots.Count; j++)
                {
                    CreatureCoverSlot slot = slots[j];
                    if (slot == null || !slot.HasStandPoint || !slot.IsFreeFor(requester))
                        continue;

                    Vector3 rawSlotPosition = slot.StandPosition;
                    float npcDistanceSqr = (rawSlotPosition - npcPosition).sqrMagnitude;

                    if (npcDistanceSqr > searchRadiusSqr)
                        continue;

                    if (!cover.IsSlotBehindEnemy(slot, enemyPosition, settings.MinBehindCoverDot))
                        continue;

                    float targetDistance = Vector3.Distance(rawSlotPosition, enemyPosition);
                    if (targetDistance < settings.MinDistance)
                        continue;

                    if (targetDistance > settings.AttackRange)
                        continue;

                    if (!NavMesh.SamplePosition(rawSlotPosition, out NavMeshHit hit, settings.NavMeshSampleRadius, settings.NavMeshAreaMask))
                        continue;

                    if (!NavMesh.CalculatePath(npcPosition, hit.position, settings.NavMeshAreaMask, path))
                        continue;

                    if (settings.RequireCompletePath && path.status != NavMeshPathStatus.PathComplete)
                        continue;

                    float npcDistance = Mathf.Sqrt(npcDistanceSqr);
                    float desiredDistancePenalty = Mathf.Abs(targetDistance - settings.DesiredDistance);
                    float score = npcDistance * settings.SlotDistanceWeight
                                  + desiredDistancePenalty * settings.DesiredDistanceWeight
                                  - slot.Priority;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestCover = cover;
                        bestSlot = slot;
                        bestNavMeshPosition = hit.position;
                    }
                }
            }

            return bestCover != null && bestSlot != null;
        }

        private void TryGenerateAutoSlots()
        {
            if (!_autoGenerateSlotsWhenEmpty)
                return;

            if (_slots == null)
                _slots = new List<CreatureCoverSlot>(4);

            if (_slots.Count > 0)
                return;

            Bounds bounds = GetWorldBounds();
            Vector3 center = _center != null ? _center.position : bounds.center;
            Vector3 forward = Flatten(transform.forward);
            Vector3 right = Flatten(transform.right);

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.right;

            forward.Normalize();
            right.Normalize();

            float forwardExtent = ProjectExtent(bounds.extents, forward);
            float rightExtent = ProjectExtent(bounds.extents, right);

            CreateAutoSlot(center, forward, right, forwardExtent, rightExtent, CreatureCoverSide.Left);
            CreateAutoSlot(center, forward, right, forwardExtent, rightExtent, CreatureCoverSide.Right);
            CreateAutoSlot(center, -forward, right, forwardExtent, rightExtent, CreatureCoverSide.Left);
            CreateAutoSlot(center, -forward, right, forwardExtent, rightExtent, CreatureCoverSide.Right);
        }

        private Bounds GetWorldBounds()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            bool hasBounds = false;
            Bounds bounds = new Bounds(transform.position, Vector3.one);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider coverCollider = colliders[i];
                if (coverCollider == null || !coverCollider.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = coverCollider.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(coverCollider.bounds);
            }

            if (hasBounds)
                return bounds;

            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private void CreateAutoSlot(
            Vector3 center,
            Vector3 coverNormal,
            Vector3 coverRight,
            float normalExtent,
            float rightExtent,
            CreatureCoverSide side)
        {
            float sideSign = side == CreatureCoverSide.Right ? 1f : -1f;
            Vector3 standPosition = center + coverNormal * (normalExtent + _autoStandOffset);
            Vector3 firePosition = center
                                   + coverNormal * (normalExtent + _autoStandOffset * 0.25f)
                                   + coverRight * sideSign * (rightExtent + _autoFireSideOffset)
                                   + Vector3.up * _autoFireHeight;

            Transform standPoint = CreateRuntimePoint($"CoverStand_{coverNormal.x:0.0}_{coverNormal.z:0.0}_{side}", standPosition);
            Transform firePoint = CreateRuntimePoint($"CoverFire_{coverNormal.x:0.0}_{coverNormal.z:0.0}_{side}", firePosition);

            _slots.Add(new CreatureCoverSlot(standPoint, firePoint, CreatureCoverStance.Standing, side));
        }

        private Transform CreateRuntimePoint(string pointName, Vector3 position)
        {
            GameObject point = new GameObject(pointName);
            point.hideFlags = HideFlags.DontSave;
            point.transform.SetParent(transform, true);
            point.transform.position = position;
            point.transform.rotation = transform.rotation;
            return point.transform;
        }

        private static float ProjectExtent(Vector3 extents, Vector3 direction)
        {
            direction = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
            return Vector3.Dot(extents, direction);
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
