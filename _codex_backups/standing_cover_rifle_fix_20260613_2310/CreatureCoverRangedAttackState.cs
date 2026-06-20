using Creatures.Navigation;
using UnityEngine;
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

            if (ShouldAbandonCover(enemy))
            {
                Context.ClearCover();
                SwitchToSearchOrOpen();
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

        private bool ShouldAbandonCover(GameObject enemy)
        {
            float enemyMoved = Vector3.Distance(enemy.transform.position, Context.EnemyPositionWhenCoverWasSelected);
            if (enemyMoved >= _settings.EnemyRelocationDistance)
                return true;

            if (!Context.CurrentCover.IsSlotBehindEnemy(Context.CurrentSlot, enemy.transform.position, _settings.MinBehindCoverDot))
                return true;

            float distanceToEnemy = Vector3.Distance(Context.CurrentSlot.StandPosition, enemy.transform.position);
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
            _phaseEndTime = Time.time + _settings.CoverFireWindow;
            TrySetAnimatorTrigger(_currentAnimation.fireState);
        }

        private void BeginHidingPhase()
        {
            _phase = CoverAttackPhase.Hiding;
            _phaseEndTime = Time.time + _settings.CoverHideDuration;
            TrySetAnimatorTrigger(_currentAnimation.hideState);
        }

        private void RunFiringPhase(GameObject enemy)
        {
            if (Time.time >= _phaseEndTime)
            {
                BeginHidingPhase();
                return;
            }

            if (_shotsThisPeek >= _settings.CoverShotsPerPeek)
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
