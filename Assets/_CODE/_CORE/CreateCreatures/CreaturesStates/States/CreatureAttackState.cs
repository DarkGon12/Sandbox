using UnityEngine;
using Zenject;


namespace Creatures.Navigation.States
{
    [CreateAssetMenu(fileName = "creatureState", menuName = "Creature/State/AttackState")]
    public class CreatureAttackState : CreatureState
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

            if (!StateView.GetSensor.HasEnemy || StateView.GetSensor.CurrentEnemy == null)
            {
                StopAnim();

                if (StateView.GetSensor.CanAgrryAll)
                {
                    StateView.GetSensor.TryAggroAll();
                    IsFinished = true;
                    return;
                }

               // StateView.GetSensor.ForceRecheck();
                IsFinished = true;
                return;
            }

            var enemy = StateView.GetSensor.CurrentEnemy;
            float dist = Vector3.Distance(StateView.transform.position, enemy.transform.position);

            if (dist > _attackDistance)
            {
                // преследуем
                StateView.GetAgent.isStopped = false;
                MoveAnim(true);
                StateView.MoveToPoint(enemy.transform);
                return;
            }

            // атакуем
            StateView.GetAgent.isStopped = true;
            MoveAnim(false);
            RotateToEnemyY(enemy.transform);

            if (Time.time >= _nextAttackTime)
            {
                _nextAttackTime = Time.time + _attackCooldown;

                StateView.GetAnimator.SetTrigger("attack");

                enemy.GetComponent<CreatureInformation>().TakeDamage(StateView.GetInfo.Damage(), StateView.GetInfo);
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

        private void RotateToEnemyY(Transform enemy)
        {
            Vector3 direction = enemy.position - StateView.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion currentRotation = StateView.transform.rotation;

            Vector3 targetEuler = targetRotation.eulerAngles;
            Vector3 currentEuler = currentRotation.eulerAngles;

            Quaternion finalRotation = Quaternion.Euler(
                currentEuler.x,
                targetEuler.y,
                currentEuler.z
            );

            StateView.transform.rotation = Quaternion.Slerp(
                currentRotation,
                finalRotation,
                7f * Time.deltaTime
            );
        }
    }
}