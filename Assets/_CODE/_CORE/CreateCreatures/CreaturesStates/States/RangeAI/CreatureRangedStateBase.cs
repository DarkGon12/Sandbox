using System.Collections.Generic;
using Sandbox.Player;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Creatures.Navigation.States
{
    public abstract class CreatureRangedStateBase : CreatureState
    {
        protected CreatureRangedCombatContext Context;
        protected CreatureRangedWeapon Weapon;
        private static readonly int DefaultLineOfSightBlockMask = Physics.DefaultRaycastLayers
                                                                  & ~(1 << 5)
                                                                  & ~(1 << 7)
                                                                  & ~(1 << 8);
        private static readonly RaycastHit[] _lineOfSightHits = new RaycastHit[16];
        private static readonly Dictionary<RuntimeAnimatorController, Dictionary<int, AnimatorControllerParameterType>> _animatorParametersByController =
            new Dictionary<RuntimeAnimatorController, Dictionary<int, AnimatorControllerParameterType>>(16);

        public override void Init(DiContainer container)
        {
            IsFinished = false;
        }

        protected void InitRangedBase(CreatureRangedCombatSettings settings)
        {
            Context = StateView.GetComponent<CreatureRangedCombatContext>();
            if (Context == null)
                Context = StateView.gameObject.AddComponent<CreatureRangedCombatContext>();

            Context.EnsureBurst(settings);
            Weapon = StateView.GetComponent<CreatureRangedWeapon>();
        }

        protected bool TryGetEnemy(out GameObject enemy)
        {
            enemy = null;

            if (StateView == null || StateView.GetSensor == null)
                return false;

            if (StateView.GetSensor.HasEnemy && StateView.GetSensor.CurrentEnemy != null)
            {
                enemy = StateView.GetSensor.CurrentEnemy.gameObject;
                return true;
            }

            if (StateView.GetSensor.HasPlayerTarget)
            {
                GameObject player = StateView.GetPlayer();
                if (player == null || !player.activeInHierarchy)
                    return false;

                enemy = player;
                return true;
            }

            return false;
        }

        protected void FinishBecauseEnemyLost()
        {
            StopMovingAnimation(null);
            Context?.ClearCover();

            if (StateView.GetSensor != null && StateView.GetSensor.CanAgrryAll)
                StateView.GetSensor.TryAggroAll();

            IsFinished = true;
        }

        protected void SetMoveAnimation(CreatureRangedCombatSettings settings, bool moving)
        {
            Animator animator = StateView.GetAnimator;
            if (animator == null)
                return;

            if (settings != null && !string.IsNullOrEmpty(settings.WalkBool))
                TrySetAnimatorBool(settings.WalkBool, moving);

            if (settings != null && !string.IsNullOrEmpty(settings.IdleBool))
                TrySetAnimatorBool(settings.IdleBool, !moving);
        }

        protected void StopMovingAnimation(CreatureRangedCombatSettings settings)
        {
            SetMoveAnimation(settings, false);
        }

        protected void SetInCoverAnimation(CreatureRangedCombatSettings settings, bool inCover, CreatureCoverSlot slot = null)
        {
            Animator animator = StateView.GetAnimator;
            if (animator == null || settings == null)
                return;

            if (!string.IsNullOrEmpty(settings.InCoverBool))
                TrySetAnimatorBool(settings.InCoverBool, inCover);

            if (slot == null)
                return;

            if (!string.IsNullOrEmpty(settings.CoverStanceInt))
                TrySetAnimatorInteger(settings.CoverStanceInt, slot.Stance == CreatureCoverStance.Standing ? 0 : 1);

            if (!string.IsNullOrEmpty(settings.CoverSideInt))
                TrySetAnimatorInteger(settings.CoverSideInt, (int)slot.Side);
        }

        protected void RotateToTargetY(Transform target, CreatureRangedCombatSettings settings)
        {
            if (target == null)
                return;

            Vector3 direction = target.position - StateView.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion currentRotation = StateView.transform.rotation;

            Vector3 targetEuler = targetRotation.eulerAngles;
            Vector3 currentEuler = currentRotation.eulerAngles;

            Quaternion finalRotation = Quaternion.Euler(currentEuler.x, targetEuler.y, currentEuler.z);
            float speed = settings != null ? settings.RotationSpeed : 7f;
            StateView.transform.rotation = Quaternion.Slerp(currentRotation, finalRotation, speed * Time.deltaTime);
        }

        protected bool IsArrived(Vector3 destination, CreatureRangedCombatSettings settings)
        {
            float arrivalDistance = settings != null ? settings.ArrivalDistance : 0.45f;

            if ((StateView.transform.position - destination).sqrMagnitude <= arrivalDistance * arrivalDistance)
                return true;

            NavMeshAgent agent = StateView.GetAgent;
            if (agent == null || agent.pathPending)
                return false;

            return agent.hasPath && agent.remainingDistance <= Mathf.Max(arrivalDistance, agent.stoppingDistance);
        }

        protected bool SetDestinationThrottled(Vector3 destination, CreatureRangedCombatSettings settings, bool force = false)
        {
            NavMeshAgent agent = StateView.GetAgent;
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return false;

            float refreshInterval = settings != null ? settings.DestinationRefreshInterval : 0.25f;

            if (!force && Time.time < Context.NextDestinationRefreshTime)
            {
                if ((Context.LastDestination - destination).sqrMagnitude < 0.25f)
                    return true;
            }

            Context.NextDestinationRefreshTime = Time.time + refreshInterval;
            Context.LastDestination = destination;
            agent.isStopped = false;
            return agent.SetDestination(destination);
        }

        protected bool TrySampleNavMesh(Vector3 position, CreatureRangedCombatSettings settings, out Vector3 sampled)
        {
            sampled = position;

            if (settings == null)
                return false;

            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, settings.NavMeshSampleRadius, settings.NavMeshAreaMask))
                return false;

            sampled = hit.position;
            return true;
        }

        protected bool TryFindRetreatPosition(GameObject enemy, CreatureRangedCombatSettings settings, out Vector3 destination)
        {
            destination = StateView.transform.position;

            if (enemy == null || settings == null)
                return false;

            Vector3 away = StateView.transform.position - enemy.transform.position;
            away.y = 0f;

            if (away.sqrMagnitude < 0.001f)
                away = -StateView.transform.forward;

            away.Normalize();

            float currentDistanceSqr = (StateView.transform.position - enemy.transform.position).sqrMagnitude;
            float[] angleOffsets = { 0f, 35f, -35f, 70f, -70f, 110f, -110f, 180f };
            NavMeshPath path = new NavMeshPath();

            for (int i = 0; i < angleOffsets.Length; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, angleOffsets[i], 0f) * away;
                Vector3 candidate = StateView.transform.position + direction * settings.RetreatStepDistance;

                if ((candidate - enemy.transform.position).sqrMagnitude <= currentDistanceSqr)
                    continue;

                if (!TrySampleNavMesh(candidate, settings, out Vector3 sampled))
                    continue;

                if (!NavMesh.CalculatePath(StateView.transform.position, sampled, settings.NavMeshAreaMask, path))
                    continue;

                if (settings.RequireCompletePath && path.status != NavMeshPathStatus.PathComplete)
                    continue;

                destination = sampled;
                return true;
            }

            return false;
        }

        protected bool TryFindApproachPosition(GameObject enemy, CreatureRangedCombatSettings settings, out Vector3 destination)
        {
            destination = StateView.transform.position;

            if (enemy == null || settings == null)
                return false;

            Vector3 fromEnemyToNpc = StateView.transform.position - enemy.transform.position;
            fromEnemyToNpc.y = 0f;

            if (fromEnemyToNpc.sqrMagnitude < 0.001f)
                fromEnemyToNpc = -enemy.transform.forward;

            fromEnemyToNpc.Normalize();

            Vector3 desiredPosition = enemy.transform.position + fromEnemyToNpc * settings.DesiredDistance;
            if (TrySampleNavMesh(desiredPosition, settings, out Vector3 sampled))
            {
                destination = sampled;
                return true;
            }

            if (TrySampleNavMesh(enemy.transform.position, settings, out sampled))
            {
                destination = sampled;
                return true;
            }

            return false;
        }

        protected Vector3 GetAimPoint(GameObject target, CreatureRangedCombatSettings settings)
        {
            float height = settings != null ? settings.TargetAimHeight : 1.2f;
            return target.transform.position + Vector3.up * height;
        }

        protected bool HasClearShot(Vector3 fireOrigin, GameObject target, CreatureRangedCombatSettings settings)
        {
            if (target == null || settings == null)
                return false;

            int mask = settings.LineOfSightBlockMask.value != 0
                ? settings.LineOfSightBlockMask.value
                : DefaultLineOfSightBlockMask;

            if (mask == 0)
                return true;

            Vector3 aimPoint = GetAimPoint(target, settings);
            Vector3 direction = aimPoint - fireOrigin;
            float distance = direction.magnitude;

            if (distance < 0.001f)
                return true;

            Vector3 normalizedDirection = direction / distance;
            int hitCount = Physics.RaycastNonAlloc(
                fireOrigin,
                normalizedDirection,
                _lineOfSightHits,
                distance,
                mask,
                QueryTriggerInteraction.Ignore);

            float closestBlockerDistance = float.MaxValue;
            float closestTargetDistance = distance;
            Transform selfRoot = StateView != null ? StateView.transform : null;
            Transform targetRoot = target.transform;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = _lineOfSightHits[i].collider;
                if (hitCollider == null)
                    continue;

                Transform hitTransform = hitCollider.transform;
                if (IsTransformInHierarchy(hitTransform, selfRoot))
                    continue;

                if (IsTransformInHierarchy(hitTransform, targetRoot))
                {
                    closestTargetDistance = Mathf.Min(closestTargetDistance, _lineOfSightHits[i].distance);
                    continue;
                }

                closestBlockerDistance = Mathf.Min(closestBlockerDistance, _lineOfSightHits[i].distance);
            }

            return closestBlockerDistance >= closestTargetDistance;
        }

        protected void PlayOpenAttackAnimation(CreatureRangedCombatSettings settings)
        {
            if (settings == null || string.IsNullOrEmpty(settings.OpenAttackTrigger))
                return;

            TrySetAnimatorTrigger(settings.OpenAttackTrigger);
        }

        protected bool TryFireShot(GameObject target, Vector3 fireOrigin, CreatureRangedCombatSettings settings, bool playOpenAttackTrigger)
        {
            if (target == null || settings == null || Context == null)
                return false;

            if (!Context.CanShootNow)
                return false;

            if (!HasClearShot(fireOrigin, target, settings))
                return false;

            if (playOpenAttackTrigger)
                PlayOpenAttackAnimation(settings);

            FireShotWithoutCooldownCheck(target, fireOrigin, settings);
            return true;
        }

        protected void FireShotWithoutCooldownCheck(GameObject target, Vector3 fireOrigin, CreatureRangedCombatSettings settings)
        {
            if (Weapon != null)
            {
                Weapon.FireAt(StateView.GetInfo, target, fireOrigin);
            }
            else if (target != null && target.TryGetComponent(out CreatureInformation targetInfo))
            {
                targetInfo.TakeDamage(StateView.GetInfo.Damage(), StateView.GetInfo);
            }
            else if (target != null && target.TryGetComponent(out PlayerInformation playerInfo))
            {
                playerInfo.TakeDamage(StateView.GetInfo.Damage());
            }

            Context.RegisterShot(settings);
        }

        protected bool TrySetAnimatorTrigger(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName))
                return false;

            Animator animator = StateView.GetAnimator;
            int parameterHash = Animator.StringToHash(triggerName);

            if (!HasAnimatorParameter(animator, parameterHash, AnimatorControllerParameterType.Trigger))
                return false;

            animator.SetTrigger(parameterHash);
            return true;
        }

        private bool TrySetAnimatorBool(string parameterName, bool value)
        {
            if (string.IsNullOrEmpty(parameterName))
                return false;

            Animator animator = StateView.GetAnimator;
            int parameterHash = Animator.StringToHash(parameterName);

            if (!HasAnimatorParameter(animator, parameterHash, AnimatorControllerParameterType.Bool))
                return false;

            animator.SetBool(parameterHash, value);
            return true;
        }

        private bool TrySetAnimatorInteger(string parameterName, int value)
        {
            if (string.IsNullOrEmpty(parameterName))
                return false;

            Animator animator = StateView.GetAnimator;
            int parameterHash = Animator.StringToHash(parameterName);

            if (!HasAnimatorParameter(animator, parameterHash, AnimatorControllerParameterType.Int))
                return false;

            animator.SetInteger(parameterHash, value);
            return true;
        }

        private static bool HasAnimatorParameter(
            Animator animator,
            int parameterHash,
            AnimatorControllerParameterType parameterType)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;

            RuntimeAnimatorController controller = animator.runtimeAnimatorController;

            if (!_animatorParametersByController.TryGetValue(controller, out Dictionary<int, AnimatorControllerParameterType> cachedParameters))
            {
                AnimatorControllerParameter[] parameters = animator.parameters;
                cachedParameters = new Dictionary<int, AnimatorControllerParameterType>(parameters.Length);

                for (int i = 0; i < parameters.Length; i++)
                    cachedParameters[parameters[i].nameHash] = parameters[i].type;

                _animatorParametersByController.Add(controller, cachedParameters);
            }

            return cachedParameters.TryGetValue(parameterHash, out AnimatorControllerParameterType actualType)
                   && actualType == parameterType;
        }

        private static bool IsTransformInHierarchy(Transform candidate, Transform root)
        {
            return candidate != null && root != null && (candidate == root || candidate.IsChildOf(root));
        }
    }
}
