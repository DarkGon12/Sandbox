using Creatures.Navigation;
using UnityEngine;
using Zenject;

namespace Creatures.Navigation.States
{
    [CreateAssetMenu(fileName = "CreatureRangedCombatState", menuName = "Creature/State/Ranged Combat Entry State")]
    public class CreatureRangedCombatState : CreatureState
    {
        [SerializeField] private CreatureState _searchCoverState;
        [SerializeField] private bool _clearPreviousCover = true;

        public override void Init(DiContainer container)
        {
            IsFinished = false;

            CreatureRangedCombatContext context = StateView.GetComponent<CreatureRangedCombatContext>();
            if (context == null)
                context = StateView.gameObject.AddComponent<CreatureRangedCombatContext>();

            if (_clearPreviousCover)
                context.ClearCover();

            if (_searchCoverState != null)
            {
                StateView.SetState(_searchCoverState);
                return;
            }

            IsFinished = true;
        }

        public override void Run()
        {
        }
    }
}
