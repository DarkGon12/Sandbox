using UnityEngine;
using Zenject;

namespace Creatures.Navigation.States
{
    [CreateAssetMenu(fileName = "creatureState", menuName = "Creature/State/ChangeTeamState")]
    public class ChangeTeamState : CreatureState
    {
        [SerializeField] private CreatureTeam _newTeam;

        public override void Init(DiContainer container)
        {
            IsFinished = true;

            StateView.GetInfo.SetTeam(_newTeam);
            StateView.SetState(StateView.GetBeforeState);
        }

        public override void Run()
        {
            if (IsFinished) return;
        }
    }
}