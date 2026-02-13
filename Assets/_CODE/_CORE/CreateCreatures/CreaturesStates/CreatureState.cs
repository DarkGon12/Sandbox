using UnityEngine;
using Zenject;

namespace Creatures.Navigation
{
    public abstract class CreatureState : ScriptableObject
    {
        public bool IsFinished { get; protected set; }
        public CreatureStateView StateView;

        public virtual void Init(DiContainer container)
        {

        }

        public abstract void Run();
    }
}