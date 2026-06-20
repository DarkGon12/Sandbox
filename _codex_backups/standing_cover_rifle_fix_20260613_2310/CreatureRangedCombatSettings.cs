using System;
using UnityEngine;
using UnityEngine.AI;

namespace Creatures.Navigation
{
    [CreateAssetMenu(fileName = "CreatureRangedCombatSettings", menuName = "Creature/Ranged Combat/Settings")]
    public class CreatureRangedCombatSettings : ScriptableObject
    {
        [Header("Distance")]
        [SerializeField] private float _desiredDistance = 8f;
        [SerializeField] private float _distanceTolerance = 1f;
        [SerializeField] private float _attackRange = 14f;
        [SerializeField] private float _retreatStepDistance = 4f;

        [Header("Cover Search")]
        [SerializeField] private float _coverSearchRadius = 12f;
        [SerializeField, Range(-1f, 1f)] private float _minBehindCoverDot = 0.35f;
        [SerializeField] private float _coverRecheckInterval = 2f;
        [SerializeField] private float _enemyRelocationDistance = 4f;
        [SerializeField] private float _slotDistanceWeight = 1f;
        [SerializeField] private float _desiredDistanceWeight = 0.6f;

        [Header("Navigation")]
        [SerializeField] private float _navMeshSampleRadius = 0.7f;
        [SerializeField] private int _navMeshAreaMask = NavMesh.AllAreas;
        [SerializeField] private bool _requireCompletePath = true;
        [SerializeField] private float _arrivalDistance = 0.45f;
        [SerializeField] private float _destinationRefreshInterval = 0.25f;

        [Header("Fire Timers")]
        [SerializeField] private int _shotsPerBurst = 1;
        [SerializeField] private float _shotInterval = 0.25f;
        [SerializeField] private float _burstCooldown = 1f;
        [SerializeField] private int _coverShotsPerPeek = 1;
        [SerializeField] private float _coverExposeDuration = 0.35f;
        [SerializeField] private float _coverFireWindow = 0.35f;
        [SerializeField] private float _coverHideDuration = 0.3f;
        [SerializeField] private float _coverHiddenPause = 0.4f;

        [Header("Rotation / Aim")]
        [SerializeField] private float _rotationSpeed = 7f;
        [SerializeField] private float _targetAimHeight = 1.2f;
        [SerializeField] private LayerMask _lineOfSightBlockMask;

        [Header("Animator Common Params")]
        [SerializeField] private string _idleBool = "idle";
        [SerializeField] private string _walkBool = "walk";
        [SerializeField] private string _openAttackTrigger = "attack";
        [SerializeField] private string _inCoverBool = "inCover";
        [SerializeField] private string _coverStanceInt = "coverStance";
        [SerializeField] private string _coverSideInt = "coverSide";

        [Header("Cover Animation Triggers")]
        [SerializeField] private CoverAnimationStateNames _coverAnimations = CoverAnimationStateNames.Default;
        [SerializeField] private float _animationCrossFadeTime = 0.08f;

        public float DesiredDistance => _desiredDistance;
        public float DistanceTolerance => _distanceTolerance;
        public float MinDistance => Mathf.Max(0f, _desiredDistance - _distanceTolerance);
        public float ComfortableMaxDistance => _desiredDistance + _distanceTolerance;
        public float AttackRange => _attackRange;
        public float RetreatStepDistance => _retreatStepDistance;

        public float CoverSearchRadius => _coverSearchRadius;
        public float MinBehindCoverDot => _minBehindCoverDot;
        public float CoverRecheckInterval => _coverRecheckInterval;
        public float EnemyRelocationDistance => _enemyRelocationDistance;
        public float SlotDistanceWeight => _slotDistanceWeight;
        public float DesiredDistanceWeight => _desiredDistanceWeight;

        public float NavMeshSampleRadius => _navMeshSampleRadius;
        public int NavMeshAreaMask => _navMeshAreaMask;
        public bool RequireCompletePath => _requireCompletePath;
        public float ArrivalDistance => _arrivalDistance;
        public float DestinationRefreshInterval => _destinationRefreshInterval;

        public int ShotsPerBurst => Mathf.Max(1, _shotsPerBurst);
        public float ShotInterval => Mathf.Max(0.01f, _shotInterval);
        public float BurstCooldown => Mathf.Max(0.01f, _burstCooldown);
        public int CoverShotsPerPeek => Mathf.Max(1, _coverShotsPerPeek);
        public float CoverExposeDuration => Mathf.Max(0f, _coverExposeDuration);
        public float CoverFireWindow => Mathf.Max(0.01f, _coverFireWindow);
        public float CoverHideDuration => Mathf.Max(0f, _coverHideDuration);
        public float CoverHiddenPause => Mathf.Max(0f, _coverHiddenPause);

        public float RotationSpeed => _rotationSpeed;
        public float TargetAimHeight => _targetAimHeight;
        public LayerMask LineOfSightBlockMask => _lineOfSightBlockMask;

        public string IdleBool => _idleBool;
        public string WalkBool => _walkBool;
        public string OpenAttackTrigger => _openAttackTrigger;
        public string InCoverBool => _inCoverBool;
        public string CoverStanceInt => _coverStanceInt;
        public string CoverSideInt => _coverSideInt;
        public float AnimationCrossFadeTime => _animationCrossFadeTime;

        public CoverAnimationTriplet GetCoverAnimation(CreatureCoverStance stance, CreatureCoverSide side)
        {
            return _coverAnimations.Get(stance, side);
        }
    }

    [Serializable]
    public struct CoverAnimationTriplet
    {
        public string showState;
        public string fireState;
        public string hideState;

        public CoverAnimationTriplet(string showState, string fireState, string hideState)
        {
            this.showState = showState;
            this.fireState = fireState;
            this.hideState = hideState;
        }
    }

    [Serializable]
    public struct CoverAnimationStateNames
    {
        public CoverAnimationTriplet standingLeft;
        public CoverAnimationTriplet standingRight;
        public CoverAnimationTriplet crouchingLeft;
        public CoverAnimationTriplet crouchingRight;

        public static CoverAnimationStateNames Default => new CoverAnimationStateNames
        {
            standingLeft = new CoverAnimationTriplet(
                "coverStandLeftShow",
                "coverStandLeftFire",
                "coverStandLeftHide"),
            standingRight = new CoverAnimationTriplet(
                "coverStandRightShow",
                "coverStandRightFire",
                "coverStandRightHide"),
            crouchingLeft = new CoverAnimationTriplet(
                "coverCrouchLeftShow",
                "coverCrouchLeftFire",
                "coverCrouchLeftHide"),
            crouchingRight = new CoverAnimationTriplet(
                "coverCrouchRightShow",
                "coverCrouchRightFire",
                "coverCrouchRightHide")
        };

        public CoverAnimationTriplet Get(CreatureCoverStance stance, CreatureCoverSide side)
        {
            if (stance == CreatureCoverStance.Standing && side == CreatureCoverSide.Left)
                return standingLeft;
            if (stance == CreatureCoverStance.Standing && side == CreatureCoverSide.Right)
                return standingRight;
            if (stance == CreatureCoverStance.Crouching && side == CreatureCoverSide.Left)
                return crouchingLeft;

            return crouchingRight;
        }
    }
}
