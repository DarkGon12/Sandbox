using Creatures.Navigation;
using UnityEngine;
using Zenject;

namespace Creatures.Navigation.States
{
    [CreateAssetMenu(fileName = "CreatureOpenRangedAttackState", menuName = "Creature/State/Open Ranged Attack State")]
    public class CreatureOpenRangedAttackState : CreatureRangedStateBase
    {
        [SerializeField] private CreatureRangedCombatSettings _settings;
        [SerializeField] private CreatureState _searchCoverState;

        public override void Init(DiContainer container)
        {
            base.Init(container);

            if (_settings == null)
            {
                IsFinished = true;
                return;
            }

            InitRangedBase(_settings);

            Context.ClearCover();
            Context.NextCoverCheckTime = Time.time + _settings.CoverRecheckInterval;

            SetInCoverAnimation(_settings, false);
            SetMoveAnimation(_settings, false);
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

            float distance = Vector3.Distance(StateView.transform.position, enemy.transform.position);

            if (Time.time >= Context.NextCoverCheckTime && _searchCoverState != null)
            {
                Context.NextCoverCheckTime = Time.time + _settings.CoverRecheckInterval;
                StateView.SetState(_searchCoverState);
                return;
            }

            RotateToTargetY(enemy.transform, _settings);

            if (distance < _settings.MinDistance)
            {
                RunAwayAndShoot(enemy);
                return;
            }

            if (distance > _settings.AttackRange)
            {
                ChaseToAttackDistance(enemy);
                return;
            }

            AttackInOpenField(enemy);
        }

        private void RunAwayAndShoot(GameObject enemy)
        {
            if (TryFindRetreatPosition(enemy, _settings, out Vector3 retreatPosition))
            {
                StateView.GetAgent.isStopped = false;
                SetMoveAnimation(_settings, true);
                SetDestinationThrottled(retreatPosition, _settings);
            }
            else
            {
                StateView.GetAgent.isStopped = true;
                SetMoveAnimation(_settings, false);
            }

            Vector3 fireOrigin = Weapon != null ? Weapon.MuzzlePosition : StateView.transform.position + Vector3.up * 1.2f;
            TryFireShot(enemy, fireOrigin, _settings, true);
        }

        private void ChaseToAttackDistance(GameObject enemy)
        {
            if (TryFindApproachPosition(enemy, _settings, out Vector3 approachPosition))
            {
                StateView.GetAgent.isStopped = false;
                SetMoveAnimation(_settings, true);
                SetDestinationThrottled(approachPosition, _settings);
                return;
            }

            StateView.GetAgent.isStopped = false;
            SetMoveAnimation(_settings, true);
            StateView.GetAgent.SetDestination(enemy.transform.position);
        }

        private void AttackInOpenField(GameObject enemy)
        {
            StateView.GetAgent.isStopped = true;
            SetMoveAnimation(_settings, false);

            Vector3 fireOrigin = Weapon != null ? Weapon.MuzzlePosition : StateView.transform.position + Vector3.up * 1.2f;
            TryFireShot(enemy, fireOrigin, _settings, true);
        }
    }
}
