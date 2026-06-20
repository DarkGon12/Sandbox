using Creatures.Navigation;
using UnityEngine;
using Zenject;

namespace Creatures.Navigation.States
{
    [CreateAssetMenu(fileName = "CreatureSearchCoverState", menuName = "Creature/State/Search Cover State")]
    public class CreatureSearchCoverState : CreatureRangedStateBase
    {
        [SerializeField] private CreatureRangedCombatSettings _settings;
        [SerializeField] private CreatureState _coverAttackState;
        [SerializeField] private CreatureState _openAttackState;

        public override void Init(DiContainer container)
        {
            base.Init(container);

            if (_settings == null)
            {
                IsFinished = true;
                return;
            }

            InitRangedBase(_settings);
            SearchCoverAndSwitchState();
        }

        public override void Run()
        {
        }

        private void SearchCoverAndSwitchState()
        {
            if (!TryGetEnemy(out GameObject enemy))
            {
                FinishBecauseEnemyLost();
                return;
            }

            Context.ClearCover();
            Vector3 npcPosition = StateView.transform.position;
            Vector3 enemyPosition = enemy.transform.position;

            bool found = CreatureCover.TryFindBestSlot(
                npcPosition,
                enemyPosition,
                _settings,
                Context,
                out CreatureCover cover,
                out CreatureCoverSlot slot,
                out Vector3 navMeshPosition);

            if (found)
            {
                Context.SetCover(cover, slot, navMeshPosition, enemyPosition);
                Context.NextCoverCheckTime = Time.time + _settings.CoverRecheckInterval;

                if (_coverAttackState != null)
                {
                    StateView.SetState(_coverAttackState);
                    return;
                }
            }

            Context.ClearCover();

            if (_openAttackState != null)
            {
                StateView.SetState(_openAttackState);
                return;
            }

            IsFinished = true;
        }
    }
}
