using Sandbox.Player;
using UnityEngine;
using Zenject;

namespace Creatures.Navigation.States
{
    [CreateAssetMenu(fileName = "creatureState", menuName = "Creature/State/AttackPlayerState")]
    public class AttackPlayerState : CreatureState
    {
        [SerializeField] private float _attackDistance = 2.2f;
        [SerializeField] private float _attackCooldown = 1f;

        private float _nextAttackTime;

        public override void Init(DiContainer container)
        {
            IsFinished = false;
            _nextAttackTime = 0f;

            StateView.GetAgent.isStopped = false;

            StateView.GetAnimator.SetBool("idle", false);
            StateView.GetAnimator.SetBool("walk", true);
        }

        public override void Run()
        {
            if (IsFinished) return;

            var enemy = StateView.GetPlayer();
            float dist = Vector3.Distance(StateView.transform.position, enemy.transform.position);

            if (enemy == null)
            {
                StopAnim();
            }

            if (dist > _attackDistance)
            {
                StateView.GetAgent.isStopped = false;
                MoveAnim(true);
                StateView.MoveToPoint(enemy.transform);
                return;
            }

            StateView.GetAgent.isStopped = true;
            MoveAnim(false);

            if (Time.time >= _nextAttackTime)
            {
                _nextAttackTime = Time.time + _attackCooldown;

                StateView.GetAnimator.SetTrigger("attack");

                if (enemy.GetComponent<PlayerInformation>() != null)
                    enemy.GetComponent<PlayerInformation>().TakeDamage(StateView.GetInfo.Damage());
                else
                    enemy.GetComponent<CreatureInformation>().TakeDamage(StateView.GetInfo.Damage());
            }
        }

        private void MoveAnim(bool moving)
        {
            StateView.GetAnimator.SetBool("walk", moving);
            StateView.GetAnimator.SetBool("idle", !moving);
        }

        private void StopAnim()
        {
            StateView.GetAnimator.SetBool("walk", false);
            StateView.GetAnimator.SetBool("idle", true);
        }
    }
}