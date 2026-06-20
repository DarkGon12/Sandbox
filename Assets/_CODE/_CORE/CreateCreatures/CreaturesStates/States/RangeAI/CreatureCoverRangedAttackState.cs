using Creatures.Navigation;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Creatures.Navigation.States
{
    [CreateAssetMenu(fileName = "CreatureCoverRangedAttackState", menuName = "Creature/State/Cover Ranged Attack State")]
    public class CreatureCoverRangedAttackState : CreatureRangedStateBase
    {
        private enum CoverAttackPhase
        {
            MovingToCover,
            Hidden,
            Showing,
            Firing,
            Hiding
        }

        [SerializeField] private CreatureRangedCombatSettings _settings;
        [SerializeField] private CreatureState _searchCoverState;
        [SerializeField] private CreatureState _openAttackState;

        private CoverAttackPhase _phase;
        private float _phaseEndTime;
        private float _nextPeekTime;
        private int _shotsThisPeek;
        private int _shotsAllowedThisPeek;
        private Vector3 _standingFirePosition;
        private bool _hasStandingFirePosition;
        private CoverAnimationTriplet _currentAnimation;

        public override void Init(DiContainer container)
        {
            base.Init(container);

            if (_settings == null)
            {
                IsFinished = true;
                return;
            }

            InitRangedBase(_settings);

            if (!Context.HasCover)
            {
                SwitchToSearchOrOpen();
                return;
            }

            _phase = CoverAttackPhase.MovingToCover;
            _phaseEndTime = 0f;
            _nextPeekTime = 0f;
            _shotsThisPeek = 0;
            _shotsAllowedThisPeek = 0;
            _standingFirePosition = Vector3.zero;
            _hasStandingFirePosition = false;
            _currentAnimation = _settings.GetCoverAnimation(Context.CurrentSlot.Stance, Context.CurrentSlot.Side);

            SetInCoverAnimation(_settings, true, Context.CurrentSlot);
            SetMoveAnimation(_settings, true);
        }

        public override void Run()
        {
            if (IsFinished)
                return;

            if (!TryGetEnemy(out GameObject enemy))
            {
                FinishBecauseEnemyLost();
                return;
            }

            if (!Context.HasCover || Context.CurrentCover == null || Context.CurrentSlot == null || !Context.CurrentSlot.HasStandPoint)
            {
                SwitchToSearchOrOpen();
                return;
            }

            bool isStandingCover = Context.CurrentSlot.Stance == CreatureCoverStance.Standing;
            if (ShouldAbandonCover(enemy, isStandingCover))
            {
                Context.ClearCover();
                SwitchToSearchOrOpen();
                return;
            }

            if (isStandingCover)
            {
                RunStandingCoverAttack(enemy);
                return;
            }

            if (!IsArrived(Context.CurrentCoverNavMeshPosition, _settings))
            {
                MoveToCover();
                return;
            }

            StateView.GetAgent.isStopped = true;
            SetMoveAnimation(_settings, false);
            RotateToTargetY(enemy.transform, _settings);

            RunCoverAttackCycle(enemy);
        }

        private void MoveToCover()
        {
            StateView.GetAgent.isStopped = false;
            SetMoveAnimation(_settings, true);
            SetDestinationThrottled(Context.CurrentCoverNavMeshPosition, _settings);
            _phase = CoverAttackPhase.MovingToCover;
        }

        private bool ShouldAbandonCover(GameObject enemy, bool isStandingCover)
        {
            float enemyMoved = Vector3.Distance(enemy.transform.position, Context.EnemyPositionWhenCoverWasSelected);
            if (enemyMoved >= _settings.EnemyRelocationDistance)
                return true;

            if (!isStandingCover
                && !Context.CurrentCover.IsSlotBehindEnemy(Context.CurrentSlot, enemy.transform.position, _settings.MinBehindCoverDot))
                return true;

            Vector3 referencePosition = isStandingCover && _hasStandingFirePosition
                ? _standingFirePosition
                : Context.CurrentSlot.StandPosition;

            float distanceToEnemy = Vector3.Distance(referencePosition, enemy.transform.position);
            if (distanceToEnemy < _settings.MinDistance)
                return true;

            if (distanceToEnemy > _settings.AttackRange)
                return true;

            return false;
        }

        private void RunCoverAttackCycle(GameObject enemy)
        {
            switch (_phase)
            {
                case CoverAttackPhase.MovingToCover:
                    BeginHiddenPhase();
                    break;

                case CoverAttackPhase.Hidden:
                    if (Time.time >= _nextPeekTime && Context.CanShootNow)
                        BeginShowingPhase();
                    break;

                case CoverAttackPhase.Showing:
                    if (Time.time >= _phaseEndTime)
                        BeginFiringPhase();
                    break;

                case CoverAttackPhase.Firing:
                    RunFiringPhase(enemy);
                    break;

                case CoverAttackPhase.Hiding:
                    if (Time.time >= _phaseEndTime)
                        BeginHiddenPhase();
                    break;
            }
        }

        private void BeginHiddenPhase()
        {
            _phase = CoverAttackPhase.Hidden;
            _nextPeekTime = Time.time + _settings.CoverHiddenPause;
        }

        private void BeginShowingPhase()
        {
            _phase = CoverAttackPhase.Showing;
            _phaseEndTime = Time.time + _settings.CoverExposeDuration;
            _shotsThisPeek = 0;
            TrySetAnimatorTrigger(_currentAnimation.showState);
        }

        private void BeginFiringPhase()
        {
            _phase = CoverAttackPhase.Firing;
            _shotsAllowedThisPeek = _settings.RollCoverShotsPerPeek();
            Context.StartBurst(_shotsAllowedThisPeek);

            float minimumFireWindow = Mathf.Max(0f, (_shotsAllowedThisPeek - 1) * _settings.ShotInterval) + 0.05f;
            _phaseEndTime = Time.time + Mathf.Max(_settings.CoverFireWindow, minimumFireWindow);
            TrySetAnimatorTrigger(_currentAnimation.fireState);
        }

        private void BeginHidingPhase()
        {
            _phase = CoverAttackPhase.Hiding;
            _phaseEndTime = Time.time + _settings.CoverHideDuration;
            TrySetAnimatorTrigger(_currentAnimation.hideState);
        }

        private void RunStandingCoverAttack(GameObject enemy)
        {
            if (!TryEnsureStandingFirePosition(enemy, out Vector3 firePosition))
            {
                Context.ClearCover();
                SwitchToSearchOrOpen();
                return;
            }

            if (!IsArrived(firePosition, _settings))
            {
                MoveToStandingFirePosition(firePosition);
                return;
            }

            StateView.GetAgent.isStopped = true;
            SetMoveAnimation(_settings, false);
            RotateToTargetY(enemy.transform, _settings);

            Vector3 fireOrigin = GetCurrentStandingFireOrigin(enemy);
            if (!HasClearShot(fireOrigin, enemy, _settings))
            {
                _hasStandingFirePosition = false;
                if (TryEnsureStandingFirePosition(enemy, out Vector3 nextFirePosition)
                    && !IsArrived(nextFirePosition, _settings))
                {
                    MoveToStandingFirePosition(nextFirePosition);
                }

                return;
            }

            TryFireShot(enemy, fireOrigin, _settings, true);
        }

        private void MoveToStandingFirePosition(Vector3 firePosition)
        {
            StateView.GetAgent.isStopped = false;
            SetMoveAnimation(_settings, true);
            SetDestinationThrottled(firePosition, _settings);
        }

        private void RunFiringPhase(GameObject enemy)
        {
            if (Time.time >= _phaseEndTime)
            {
                BeginHidingPhase();
                return;
            }

            if (_shotsThisPeek >= _shotsAllowedThisPeek)
            {
                BeginHidingPhase();
                return;
            }

            Vector3 fireOrigin = Context.CurrentSlot.HasFirePoint
                ? Context.CurrentSlot.FirePosition
                : Weapon != null
                    ? Weapon.MuzzlePosition
                    : StateView.transform.position + Vector3.up * 1.2f;

            if (TryFireShot(enemy, fireOrigin, _settings, false))
            {
                _shotsThisPeek++;
                if (!TrySetAnimatorTrigger(_currentAnimation.fireState))
                    PlayOpenAttackAnimation(_settings);
            }
        }

        private bool TryEnsureStandingFirePosition(GameObject enemy, out Vector3 firePosition)
        {
            firePosition = _standingFirePosition;

            if (_hasStandingFirePosition && IsStandingFirePositionUsable(_standingFirePosition, enemy))
                return true;

            _hasStandingFirePosition = TryFindStandingFirePosition(enemy, out _standingFirePosition);
            firePosition = _standingFirePosition;
            return _hasStandingFirePosition;
        }

        private bool TryFindStandingFirePosition(GameObject enemy, out Vector3 firePosition)
        {
            firePosition = Vector3.zero;

            if (enemy == null || !Context.HasCover)
                return false;

            NavMeshPath path = new NavMeshPath();
            CreatureCoverSlot slot = Context.CurrentSlot;

            if (slot.HasFirePoint && TryUseStandingCandidate(slot.FirePosition, enemy, path, out firePosition))
                return true;

            if (TryUseStandingCandidate(slot.StandPosition, enemy, path, out firePosition))
                return true;

            Vector3 coverCenter = Context.CurrentCover.Center.position;
            Bounds coverBounds = GetCoverWorldBounds(Context.CurrentCover);
            Vector3 toEnemy = Flatten(enemy.transform.position - coverCenter);
            if (toEnemy.sqrMagnitude < 0.001f)
                toEnemy = Flatten(enemy.transform.position - StateView.transform.position);

            if (toEnemy.sqrMagnitude < 0.001f)
                toEnemy = StateView.transform.forward;

            toEnemy.Normalize();

            Vector3 awayFromEnemy = -toEnemy;
            Vector3 sideDirection = Vector3.Cross(Vector3.up, toEnemy);
            if (sideDirection.sqrMagnitude < 0.001f)
                sideDirection = StateView.transform.right;

            sideDirection.Normalize();

            float sideExtent = ProjectBoundsExtent(coverBounds, sideDirection);
            float backExtent = ProjectBoundsExtent(coverBounds, toEnemy);
            Vector3 basePosition = coverCenter
                                   + awayFromEnemy * (backExtent + _settings.StandingCoverBackOffset);

            float preferredSide = slot.Side == CreatureCoverSide.Right ? 1f : -1f;
            float[] sideSigns = { preferredSide, -preferredSide };
            float[] additionalBackOffsets =
            {
                0f,
                _settings.StandingCoverSideSearchStep,
                -_settings.StandingCoverSideSearchStep
            };

            float step = _settings.StandingCoverSideSearchStep;
            float startSideDistance = Mathf.Max(step, sideExtent + step);
            float maxDistance = sideExtent + _settings.StandingCoverSideSearchDistance;

            for (int sideIndex = 0; sideIndex < sideSigns.Length; sideIndex++)
            {
                Vector3 signedSideDirection = sideDirection * sideSigns[sideIndex];

                for (float sideDistance = startSideDistance; sideDistance <= maxDistance + 0.001f; sideDistance += step)
                {
                    for (int backIndex = 0; backIndex < additionalBackOffsets.Length; backIndex++)
                    {
                        Vector3 candidate = basePosition
                                            + signedSideDirection * sideDistance
                                            + awayFromEnemy * additionalBackOffsets[backIndex];

                        if (TryUseStandingCandidate(candidate, enemy, path, out firePosition))
                            return true;
                    }
                }
            }

            return false;
        }

        private bool TryUseStandingCandidate(Vector3 candidate, GameObject enemy, NavMeshPath path, out Vector3 sampledPosition)
        {
            sampledPosition = candidate;

            if (!TrySampleNavMesh(candidate, _settings, out sampledPosition))
                return false;

            if (!NavMesh.CalculatePath(StateView.transform.position, sampledPosition, _settings.NavMeshAreaMask, path))
                return false;

            if (_settings.RequireCompletePath && path.status != NavMeshPathStatus.PathComplete)
                return false;

            return IsStandingFirePositionUsable(sampledPosition, enemy);
        }

        private bool IsStandingFirePositionUsable(Vector3 position, GameObject enemy)
        {
            Vector3 fireOrigin = GetStandingFireOrigin(position);
            return HasClearShot(fireOrigin, enemy, _settings);
        }

        private Vector3 GetStandingFireOrigin(Vector3 position)
        {
            return position + Vector3.up * _settings.StandingCoverFireHeight;
        }

        private Vector3 GetCurrentStandingFireOrigin(GameObject enemy)
        {
            if (Weapon != null && Weapon.HasMuzzlePoint && HasClearShot(Weapon.MuzzlePosition, enemy, _settings))
                return Weapon.MuzzlePosition;

            return GetStandingFireOrigin(StateView.transform.position);
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private static Bounds GetCoverWorldBounds(CreatureCover cover)
        {
            if (cover == null)
                return new Bounds(Vector3.zero, Vector3.one);

            Collider[] colliders = cover.GetComponentsInChildren<Collider>();
            bool hasBounds = false;
            Bounds bounds = new Bounds(cover.Center.position, Vector3.one);

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

            Renderer[] renderers = cover.GetComponentsInChildren<Renderer>();
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

        private static float ProjectBoundsExtent(Bounds bounds, Vector3 direction)
        {
            direction.Normalize();
            direction = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
            return Vector3.Dot(bounds.extents, direction);
        }

        private void SwitchToSearchOrOpen()
        {
            if (_searchCoverState != null)
            {
                StateView.SetState(_searchCoverState);
                return;
            }

            if (_openAttackState != null)
            {
                StateView.SetState(_openAttackState);
                return;
            }

            IsFinished = true;
        }
    }
}
